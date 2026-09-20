using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Maintenance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record GetMaintenanceOrdersQuery(
    Guid CompanyId, int PageNumber = 1, int PageSize = 50, Guid? AssetId = null, MaintenanceOrderStatus? Status = null)
    : IRequest<PagedResult<MaintenanceOrderSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Maintenance.Read;
}

public sealed record MaintenanceOrderSummary(
    Guid Id, Guid AssetId, string AssetFolio, string Folio, MaintenanceOrderType Type, MaintenanceOrderStatus Status,
    DateTimeOffset OpenedAtUtc, DateTimeOffset? ClosedAtUtc);

public sealed class GetMaintenanceOrdersQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetMaintenanceOrdersQuery, PagedResult<MaintenanceOrderSummary>>
{
    public Task<PagedResult<MaintenanceOrderSummary>> Handle(GetMaintenanceOrdersQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var query = db.MaintenanceOrders.AsNoTracking().Where(o => o.CompanyId == request.CompanyId);

        if (request.AssetId is { } assetId)
        {
            query = query.Where(o => o.AssetId == assetId);
        }

        if (request.Status is { } status)
        {
            query = query.Where(o => o.Status == status);
        }

        var projected =
            from o in query
            join asset in db.Assets.AsNoTracking() on o.AssetId equals asset.Id
            orderby o.OpenedAtUtc descending
            select new MaintenanceOrderSummary(
                o.Id, o.AssetId, asset.InternalFolio, o.Folio, o.Type, o.Status, o.OpenedAtUtc, o.ClosedAtUtc);

        return PagedResult<MaintenanceOrderSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
