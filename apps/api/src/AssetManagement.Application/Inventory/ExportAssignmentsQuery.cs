using AssetManagement.Application.Common;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Application.ImportExport;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>Same filters as <see cref="GetAssignmentsQuery"/> (F3), capped instead of paginated — same
/// 10,000-row cap ExportAssetsQuery (F9/F10) already uses, same reasoning (ADR 0011 decision 7: no write
/// scale here to justify a queue, this is synchronous and read-only).</summary>
public sealed record ExportAssignmentsQuery(
    Guid CompanyId,
    ExportFileFormat Format,
    AssignmentStatus? Status = null,
    Guid? AssetId = null,
    string? AssignedToSearch = null,
    string? Search = null,
    DateOnly? AssignedFrom = null,
    DateOnly? AssignedTo = null,
    Guid? OrgUnitId = null)
    : IRequest<ExportFileResult>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Exports.Create;
}

public sealed class ExportAssignmentsQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany, IClock clock)
    : IRequestHandler<ExportAssignmentsQuery, ExportFileResult>
{
    private const int MaxRows = 10_000;

    private static readonly string[] Headers =
    [
        "Folio", "Clave del activo", "Marca", "Modelo", "Número de serie", "Estado del activo",
        "Asignado a", "Área", "Estado de la asignación", "Fecha de asignación",
        "Fecha de aceptación", "Notas",
    ];

    // A4 landscape usable width (782pt) — "el reporte en pdf ponlo de manera horizontal" — with wrapping
    // enabled (see BuildPdf's wrapText) so every field shows in full instead of getting truncated.
    private static readonly double[] ColumnWidths = [65, 75, 45, 60, 65, 60, 75, 70, 70, 55, 55, 87];

    // Mismas etiquetas en español que la UI (apps/web/src/lib/asset-labels.ts /
    // apps/web/src/lib/inventory-labels.ts) — un reporte no debe mostrar los valores crudos del enum.
    private static readonly Dictionary<AssignmentStatus, string> AssignmentStatusLabels = new()
    {
        [AssignmentStatus.PendingSignature] = "Pendiente de firma",
        [AssignmentStatus.Accepted] = "Aceptada",
        [AssignmentStatus.Returned] = "Devuelta",
        [AssignmentStatus.Cancelled] = "Cancelada",
    };

    private static readonly Dictionary<Domain.Assets.AssetStatus, string> AssetStatusLabels = new()
    {
        [Domain.Assets.AssetStatus.InWarehouse] = "En almacén",
        [Domain.Assets.AssetStatus.Reserved] = "Reservado",
        [Domain.Assets.AssetStatus.Assigned] = "Asignado",
        [Domain.Assets.AssetStatus.OnLoan] = "Prestado",
        [Domain.Assets.AssetStatus.InTransit] = "En tránsito",
        [Domain.Assets.AssetStatus.InMaintenance] = "En mantenimiento",
        [Domain.Assets.AssetStatus.UnderWarranty] = "En garantía",
        [Domain.Assets.AssetStatus.Damaged] = "Dañado",
        [Domain.Assets.AssetStatus.Lost] = "Extraviado",
        [Domain.Assets.AssetStatus.Stolen] = "Robado",
        [Domain.Assets.AssetStatus.PendingDecommission] = "Pendiente de baja",
        [Domain.Assets.AssetStatus.Decommissioned] = "Dado de baja",
        [Domain.Assets.AssetStatus.Sold] = "Vendido",
        [Domain.Assets.AssetStatus.Donated] = "Donado",
        [Domain.Assets.AssetStatus.Destroyed] = "Destruido",
    };

    public async Task<ExportFileResult> Handle(ExportAssignmentsQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var query = db.Assignments.AsNoTracking().Where(a => a.CompanyId == request.CompanyId);

        if (request.Status is { } status)
        {
            query = query.Where(a => a.Status == status);
        }

        if (request.AssetId is { } assetId)
        {
            query = query.Where(a => a.AssetId == assetId);
        }

        if (request.AssignedFrom is { } assignedFrom)
        {
            var fromUtc = assignedFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.AssignedAtUtc >= fromUtc);
        }

        if (request.AssignedTo is { } assignedTo)
        {
            var toUtc = assignedTo.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(a => a.AssignedAtUtc <= toUtc);
        }

        if (request.OrgUnitId is { } orgUnitId)
        {
            query = query.Where(a => a.OrgUnitId == orgUnitId);
        }

        var joined =
            from a in query
            join asset in db.Assets.AsNoTracking() on a.AssetId equals asset.Id
            join user in db.Users.AsNoTracking() on a.AssignedToUserId equals user.Id
            join tag in db.AssetTags.AsNoTracking() on asset.Id equals tag.AssetId into tags
            from tag in tags.DefaultIfEmpty()
            join orgUnit in db.OrgUnits.AsNoTracking() on a.OrgUnitId equals (Guid?)orgUnit.Id into orgUnits
            from orgUnit in orgUnits.DefaultIfEmpty()
            join movement in db.Movements.AsNoTracking() on a.MovementId equals movement.Id
            select new
            {
                Assignment = a,
                asset.InternalFolio,
                TagCode = tag != null ? tag.Code : null,
                asset.Brand,
                asset.Model,
                asset.SerialNumber,
                AssetStatus = asset.Status,
                AssignedToName = user.DisplayName,
                OrgUnitName = orgUnit != null ? orgUnit.Name : null,
                movement.Notes,
            };

        if (!string.IsNullOrWhiteSpace(request.AssignedToSearch))
        {
            var term = request.AssignedToSearch.Trim();
            joined = joined.Where(x => x.AssignedToName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            joined = joined.Where(x => x.InternalFolio.Contains(term) || x.Brand.Contains(term) || x.Model.Contains(term));
        }

        var rows = await joined
            .OrderByDescending(x => x.Assignment.AssignedAtUtc)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        var timestamp = clock.UtcNow.ToString("yyyyMMdd-HHmmss");

        var exportRows = rows.Select(r => new[]
        {
            r.InternalFolio,
            r.TagCode ?? "",
            r.Brand,
            r.Model,
            r.SerialNumber ?? "",
            AssetStatusLabels.GetValueOrDefault(r.AssetStatus, r.AssetStatus.ToString()),
            r.AssignedToName,
            r.OrgUnitName ?? "",
            AssignmentStatusLabels.GetValueOrDefault(r.Assignment.Status, r.Assignment.Status.ToString()),
            r.Assignment.AssignedAtUtc.ToString("yyyy-MM-dd"),
            r.Assignment.AcceptedAtUtc?.ToString("yyyy-MM-dd") ?? "",
            r.Notes ?? "",
        });

        return request.Format switch
        {
            ExportFileFormat.Xlsx => new ExportFileResult(
                TabularFileBuilder.BuildXlsx("Asignaciones", Headers, exportRows),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"asignaciones-{timestamp}.xlsx"),
            ExportFileFormat.Pdf => new ExportFileResult(
                TabularFileBuilder.BuildPdf(
                    "Reporte de asignaciones", Headers, exportRows, ColumnWidths, landscape: true, wrapText: true),
                "application/pdf",
                $"asignaciones-{timestamp}.pdf"),
            _ => throw new ConflictException("Formato de exportación no soportado."),
        };
    }
}
