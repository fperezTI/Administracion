using System.Globalization;
using System.Text.Json;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.ImportExport;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.ImportExport;

/// <summary>
/// Runs the two phases of a bulk asset import (ADR 0011) — invoked by
/// <c>ImportBatchBackgroundService</c>, never directly through <c>ISender</c> (there is no HTTP request
/// behind it). <b>Every read here uses <c>IgnoreQueryFilters()</c></b>: <c>Asset</c>/<c>OrgUnit</c>/
/// <c>ImportBatch</c> carry a company query filter driven by <c>ICurrentCompanyContext.AccessibleCompanyIds</c>,
/// which reads from the current <c>HttpContext</c> — nonexistent in a background worker's DI scope, so
/// the filter would otherwise evaluate to "no companies" and silently return zero rows. Every query below
/// instead trusts the batch's own <c>CompanyId</c> explicitly, the same "never rely on ambient company"
/// principle ADR 0010 already established. <c>AssetCategory</c>/<c>CustomFieldDefinition</c> are global
/// catalogs with no company filter, so no special handling is needed for those.
/// </summary>
public sealed class ImportBatchProcessor(IApplicationDbContext db, IFileStorage fileStorage, IFolioGenerator folioGenerator, IClock clock)
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    public async Task ValidateAsync(Guid importBatchId, CancellationToken cancellationToken)
    {
        var batch = await db.ImportBatches.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == importBatchId, cancellationToken);
        if (batch is null || batch.Status != ImportBatchStatus.Queued)
        {
            return;
        }

        batch.BeginValidation(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var rows = await ReadRowsAsync(batch.BlobPath, cancellationToken);
            var validator = await BuildValidatorAsync(batch.CompanyId, cancellationToken);

            var results = new List<ImportRowResult>(rows.Count);
            var validCount = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                var (resolved, errors) = validator.Validate(rows[i]);
                results.Add(resolved is not null ? ImportRowResult.Ok(i + 1) : ImportRowResult.Fail(i + 1, errors));
                if (resolved is not null)
                {
                    validCount++;
                }
            }

            batch.CompleteValidation(rows.Count, validCount, rows.Count - validCount, JsonSerializer.Serialize(results, SerializerOptions), clock.UtcNow);
        }
        catch (Exception ex)
        {
            batch.FailValidation(ex.Message, clock.UtcNow);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CommitAsync(Guid importBatchId, CancellationToken cancellationToken)
    {
        var batch = await db.ImportBatches.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == importBatchId, cancellationToken);
        if (batch is null || batch.Status != ImportBatchStatus.Processing)
        {
            return;
        }

        try
        {
            var rows = await ReadRowsAsync(batch.BlobPath, cancellationToken);
            var validator = await BuildValidatorAsync(batch.CompanyId, cancellationToken);
            var categoriesById = await db.AssetCategories.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken);

            var results = new ImportRowResult[rows.Count];
            var resolvedRows = new (int RowNumber, ResolvedAssetImportRow Resolved)[rows.Count];
            var failedCount = 0;

            for (var i = 0; i < rows.Count; i++)
            {
                var rowNumber = i + 1;
                var (resolved, errors) = validator.Validate(rows[i]);
                if (resolved is null)
                {
                    results[i] = ImportRowResult.Fail(rowNumber, errors);
                    failedCount++;
                }
                else
                {
                    results[i] = ImportRowResult.Ok(rowNumber);
                    resolvedRows[i] = (rowNumber, resolved);
                }
            }

            // AllOrNothing re-validates at commit time (state can drift since the preview, same risk
            // F4/F5/F7 already accept) — if anything is now invalid, nothing is created at all.
            if (batch.CommitMode == ImportCommitMode.AllOrNothing && failedCount > 0)
            {
                batch.FailProcessing(
                    "El lote tiene filas inválidas al confirmar (el estado pudo cambiar desde la vista previa); no se creó ningún activo.",
                    JsonSerializer.Serialize(results, SerializerOptions), clock.UtcNow);
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            var succeeded = 0;
            var now = clock.UtcNow;
            for (var i = 0; i < rows.Count; i++)
            {
                if (resolvedRows[i].Resolved is not { } resolved)
                {
                    continue;
                }

                var folio = await folioGenerator.NextAsync(batch.CompanyId, FolioDocumentTypes.Asset, cancellationToken);
                var asset = Asset.Create(
                    batch.CompanyId, resolved.AssetCategoryId, folio, resolved.Brand, resolved.Model, resolved.SerialNumber,
                    resolved.Description, resolved.PhysicalCondition, now, batch.CreatedByUserId);

                if (resolved.CurrentOrgUnitId is not null)
                {
                    asset.MoveToOrgUnit(resolved.CurrentOrgUnitId, now, batch.CreatedByUserId);
                }

                if (resolved.CustomFieldValues.Count != 0)
                {
                    asset.SetCustomFieldValues(resolved.CustomFieldValues.Select(kv => (kv.Key, kv.Value)));
                }

                var category = categoriesById[resolved.AssetCategoryId];
                var technology = resolved.IdentificationTechnologyOverride ?? category.DefaultIdentificationTechnology;
                asset.IssueTag(Guid.NewGuid().ToString("N"), technology, now);

                db.Assets.Add(asset);
                results[resolvedRows[i].RowNumber - 1] = ImportRowResult.Ok(resolvedRows[i].RowNumber, folio);
                succeeded++;
            }

            batch.CompleteProcessing(succeeded, failedCount, JsonSerializer.Serialize(results, SerializerOptions), clock.UtcNow);
        }
        catch (Exception ex)
        {
            batch.FailProcessing(ex.Message, reportJson: null, clock.UtcNow);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AssetImportRowValidator> BuildValidatorAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var categories = await db.AssetCategories.AsNoTracking().Include(c => c.CustomFields).ToListAsync(cancellationToken);
        var categoriesByCode = categories.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

        var orgUnits = await db.OrgUnits.IgnoreQueryFilters().AsNoTracking()
            .Where(o => o.CompanyId == companyId)
            .ToListAsync(cancellationToken);
        var orgUnitIdsByCode = orgUnits.ToDictionary(o => o.Code, o => o.Id, StringComparer.OrdinalIgnoreCase);

        var existingSerialNumbers = await db.Assets.IgnoreQueryFilters().AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.SerialNumber != null)
            .Select(a => a.SerialNumber!)
            .ToListAsync(cancellationToken);

        return new AssetImportRowValidator(categoriesByCode, orgUnitIdsByCode, new HashSet<string>(existingSerialNumbers, StringComparer.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> ReadRowsAsync(string blobPath, CancellationToken cancellationToken)
    {
        await using var stream = await fileStorage.OpenReadAsync(blobPath, cancellationToken);
        using var reader = new StreamReader(stream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { MissingFieldFound = null, HeaderValidated = null };
        using var csv = new CsvReader(reader, config);

        var rows = new List<IReadOnlyDictionary<string, string>>();
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];

        while (await csv.ReadAsync())
        {
            var record = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                record[header] = csv.GetField(header) ?? string.Empty;
            }

            rows.Add(record);
        }

        return rows;
    }
}
