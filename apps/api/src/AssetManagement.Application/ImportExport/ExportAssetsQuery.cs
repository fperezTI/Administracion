using AssetManagement.Application.Common;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.ImportExport;

public enum ExportFileFormat
{
    Xlsx,
    Pdf,
}

/// <summary>Synchronous and streamed, unlike importing — read-only, no 10,000-row write scale to justify
/// a queue (ADR 0011 decision 7). Same filters as <c>GetAssetsQuery</c> (F2), capped instead of paginated
/// (same 10,000-row cap importing uses, documented, not a hard technical limit).</summary>
public sealed record ExportAssetsQuery(
    Guid CompanyId, ExportFileFormat Format, Guid? AssetCategoryId = null, AssetStatus? Status = null, string? Search = null)
    : IRequest<ExportFileResult>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Exports.Create;
}

public sealed record ExportFileResult(byte[] Content, string ContentType, string FileName);

public sealed class ExportAssetsQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany, IClock clock)
    : IRequestHandler<ExportAssetsQuery, ExportFileResult>
{
    private const int MaxRows = 10_000;

    private static readonly string[] Headers =
        ["Folio", "Categoría", "Marca", "Modelo", "Número de serie", "Estado", "Condición física", "Ubicación"];

    private static readonly double[] ColumnWidths = [70, 90, 80, 80, 80, 70, 70, 90];

    public async Task<ExportFileResult> Handle(ExportAssetsQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var query = db.Assets.AsNoTracking().Where(a => a.CompanyId == request.CompanyId);

        if (request.AssetCategoryId is { } categoryId)
        {
            query = query.Where(a => a.AssetCategoryId == categoryId);
        }

        if (request.Status is { } status)
        {
            query = query.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(a =>
                a.InternalFolio.Contains(term) || a.Brand.Contains(term) || a.Model.Contains(term) ||
                (a.SerialNumber != null && a.SerialNumber.Contains(term)));
        }

        var rows = await (
            from a in query
            join category in db.AssetCategories.AsNoTracking() on a.AssetCategoryId equals category.Id
            join orgUnit in db.OrgUnits.AsNoTracking() on a.CurrentOrgUnitId equals (Guid?)orgUnit.Id into orgUnits
            from orgUnit in orgUnits.DefaultIfEmpty()
            orderby a.InternalFolio
            select new
            {
                a.InternalFolio,
                CategoryName = category.Name,
                a.Brand,
                a.Model,
                a.SerialNumber,
                a.Status,
                a.PhysicalCondition,
                OrgUnitName = orgUnit != null ? orgUnit.Name : null,
            })
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var timestamp = clock.UtcNow.ToString("yyyyMMdd-HHmmss");

        var exportRows = rows.Select(r => new[]
        {
            r.InternalFolio, r.CategoryName, r.Brand, r.Model, r.SerialNumber ?? "", r.Status.ToString(),
            r.PhysicalCondition.ToString(), r.OrgUnitName ?? "",
        });

        return request.Format switch
        {
            ExportFileFormat.Xlsx => new ExportFileResult(
                TabularFileBuilder.BuildXlsx("Activos", Headers, exportRows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"activos-{timestamp}.xlsx"),
            ExportFileFormat.Pdf => new ExportFileResult(
                TabularFileBuilder.BuildPdf("Listado de activos", Headers, exportRows, ColumnWidths),
                "application/pdf",
                $"activos-{timestamp}.pdf"),
            _ => throw new ConflictException("Formato de exportación no soportado."),
        };
    }
}
