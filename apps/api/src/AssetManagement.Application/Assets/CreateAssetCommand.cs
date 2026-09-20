using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Organization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets;

/// <summary>Alta y etiquetado de un activo (pedido §10/§11/§12 — one of the mandatory E2E flows, pedido
/// §35). Generates the internal folio, creates the asset in InWarehouse, and issues its identification
/// tag in the same operation — receiving and labeling an asset is one real-world action.</summary>
public sealed record CreateAssetCommand(
    Guid CompanyId,
    Guid AssetCategoryId,
    string Brand,
    string Model,
    string? SerialNumber,
    string? Description,
    PhysicalCondition PhysicalCondition,
    Guid? CurrentOrgUnitId,
    IdentificationTechnology? IdentificationTechnologyOverride,
    IReadOnlyDictionary<Guid, string>? CustomFieldValues)
    : IRequest<CreateAssetResult>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Assets.Create;
}

public sealed record CreateAssetResult(Guid AssetId, string InternalFolio, string TagCode);

public sealed class CreateAssetCommandValidator : AbstractValidator<CreateAssetCommand>
{
    public CreateAssetCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.AssetCategoryId).NotEmpty();
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class CreateAssetCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IFolioGenerator folioGenerator, IClock clock)
    : IRequestHandler<CreateAssetCommand, CreateAssetResult>
{
    public async Task<CreateAssetResult> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var category = await db.AssetCategories
            .FirstOrDefaultAsync(c => c.Id == request.AssetCategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(AssetCategory), request.AssetCategoryId);

        if (!category.IsActive)
        {
            throw new ConflictException("La categoría indicada está desactivada.");
        }

        if (request.CurrentOrgUnitId is { } orgUnitId)
        {
            var orgUnitExists = await db.OrgUnits
                .AnyAsync(o => o.Id == orgUnitId && o.CompanyId == request.CompanyId, cancellationToken);
            if (!orgUnitExists)
            {
                throw new NotFoundException(nameof(OrgUnit), orgUnitId);
            }
        }

        var fieldDefinitions = await db.CustomFieldDefinitions
            .Where(f => f.AssetCategoryId == request.AssetCategoryId)
            .ToListAsync(cancellationToken);

        var providedValues = request.CustomFieldValues ?? new Dictionary<Guid, string>();
        var missingRequired = fieldDefinitions
            .Where(f => f.IsRequired && !providedValues.ContainsKey(f.Id))
            .Select(f => f.Name)
            .ToList();
        if (missingRequired.Count != 0)
        {
            throw new ConflictException(
                $"Faltan campos obligatorios de la categoría: {string.Join(", ", missingRequired)}.");
        }

        var now = clock.UtcNow;
        var internalFolio = await folioGenerator.NextAsync(request.CompanyId, FolioDocumentTypes.Asset, cancellationToken);

        var asset = Asset.Create(
            request.CompanyId, request.AssetCategoryId, internalFolio, request.Brand, request.Model,
            request.SerialNumber, request.Description, request.PhysicalCondition, now, currentUser.UserId);

        if (request.CurrentOrgUnitId is not null)
        {
            asset.MoveToOrgUnit(request.CurrentOrgUnitId, now, currentUser.UserId);
        }

        if (providedValues.Count != 0)
        {
            var validFieldIds = fieldDefinitions.Select(f => f.Id).ToHashSet();
            var invalidKeys = providedValues.Keys.Where(k => !validFieldIds.Contains(k)).ToList();
            if (invalidKeys.Count != 0)
            {
                throw new ConflictException("Uno o más campos personalizados no pertenecen a esta categoría.");
            }

            asset.SetCustomFieldValues(providedValues.Select(kv => (kv.Key, kv.Value)));
        }

        // The tag's machine-readable code is deliberately NOT the internal folio: the folio is only
        // unique per company (pedido §8), but a QR/barcode payload must resolve unambiguously across
        // every company in this single database (pedido §12 lists "folio visible" and "identificador
        // legible por máquina" as two separate pieces of label content) — the folio is still shown on
        // the label as the human-readable text.
        var technology = request.IdentificationTechnologyOverride ?? category.DefaultIdentificationTechnology;
        var tagCode = Guid.NewGuid().ToString("N");
        var tag = asset.IssueTag(tagCode, technology, now);

        db.Assets.Add(asset);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateAssetResult(asset.Id, asset.InternalFolio, tag.Code);
    }
}
