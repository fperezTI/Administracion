using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.SparePartsAndConsumables;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.SparePartsAndConsumables;

public sealed record GetSparePartsQuery(Guid CompanyId, SparePartStatus? Status = null)
    : IRequest<IReadOnlyList<SparePartSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.SpareParts.Read;
}

public sealed record SparePartSummary(
    Guid Id, string Name, string? PartNumber, string SerialNumber, SparePartStatus Status, Guid? CurrentAssetId,
    Guid? CurrentWarehouseOrgUnitId);

public sealed class GetSparePartsQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetSparePartsQuery, IReadOnlyList<SparePartSummary>>
{
    public async Task<IReadOnlyList<SparePartSummary>> Handle(GetSparePartsQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var query = db.SpareParts.AsNoTracking().Where(p => p.CompanyId == request.CompanyId);

        if (request.Status is { } status)
        {
            query = query.Where(p => p.Status == status);
        }

        return await query.OrderBy(p => p.Name)
            .Select(p => new SparePartSummary(p.Id, p.Name, p.PartNumber, p.SerialNumber, p.Status, p.CurrentAssetId, p.CurrentWarehouseOrgUnitId))
            .ToListAsync(cancellationToken);
    }
}
