using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.SparePartsAndConsumables;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.SparePartsAndConsumables;

public sealed record GetSparePartByIdQuery(Guid SparePartId) : IRequest<SparePartDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.SpareParts.Read;
}

public sealed record SparePartInstallationInfo(
    Guid AssetId, string AssetFolio, Guid? MaintenanceOrderId, DateTimeOffset InstalledAtUtc, DateTimeOffset? RemovedAtUtc);

public sealed record SparePartDetail(
    Guid Id, Guid CompanyId, string Name, string? PartNumber, string SerialNumber, SparePartStatus Status,
    Guid? CurrentAssetId, Guid? CurrentWarehouseOrgUnitId, IReadOnlyList<SparePartInstallationInfo> Installations);

public sealed class GetSparePartByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetSparePartByIdQuery, SparePartDetail>
{
    public async Task<SparePartDetail> Handle(GetSparePartByIdQuery request, CancellationToken cancellationToken)
    {
        var sparePart = await db.SpareParts.AsNoTracking().Include(p => p.Installations)
            .FirstOrDefaultAsync(p => p.Id == request.SparePartId, cancellationToken)
            ?? throw new NotFoundException(nameof(SparePart), request.SparePartId);

        // IgnoreQueryFilters: an asset an old installation points to may since have transferred to a
        // different company (F5) — this history must stay visible regardless of the caller's active company.
        var assetIds = sparePart.Installations.Select(i => i.AssetId).Distinct().ToList();
        var assetFolios = await db.Assets.AsNoTracking().IgnoreQueryFilters()
            .Where(a => assetIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.InternalFolio, cancellationToken);

        return new SparePartDetail(
            sparePart.Id, sparePart.CompanyId, sparePart.Name, sparePart.PartNumber, sparePart.SerialNumber,
            sparePart.Status, sparePart.CurrentAssetId, sparePart.CurrentWarehouseOrgUnitId,
            sparePart.Installations.OrderByDescending(i => i.InstalledAtUtc)
                .Select(i => new SparePartInstallationInfo(
                    i.AssetId, assetFolios.GetValueOrDefault(i.AssetId, "(activo eliminado)"), i.MaintenanceOrderId,
                    i.InstalledAtUtc, i.RemovedAtUtc))
                .ToList());
    }
}
