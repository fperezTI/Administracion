using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.SparePartsAndConsumables;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.SparePartsAndConsumables;

public sealed record GetSparePartsQuery(
    Guid CompanyId,
    SparePartStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 50,
    /// <summary>name (default) | serialNumber | status</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<SparePartSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.SpareParts.Read;
}

public sealed record SparePartSummary(
    Guid Id, string Name, string? PartNumber, string SerialNumber, SparePartStatus Status, Guid? CurrentAssetId,
    Guid? CurrentWarehouseOrgUnitId);

public sealed class GetSparePartsQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetSparePartsQuery, PagedResult<SparePartSummary>>
{
    public Task<PagedResult<SparePartSummary>> Handle(GetSparePartsQuery request, CancellationToken cancellationToken)
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

        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "serialNumber" => descending ? query.OrderByDescending(p => p.SerialNumber) : query.OrderBy(p => p.SerialNumber),
            "status" => descending ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
            "name" => descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            _ => query.OrderBy(p => p.Name),
        };

        var projected = ordered.Select(p => new SparePartSummary(
            p.Id, p.Name, p.PartNumber, p.SerialNumber, p.Status, p.CurrentAssetId, p.CurrentWarehouseOrgUnitId));

        return PagedResult<SparePartSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
