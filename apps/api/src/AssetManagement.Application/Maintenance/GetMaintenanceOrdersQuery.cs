using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Maintenance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record GetMaintenanceOrdersQuery(
    Guid CompanyId,
    int PageNumber = 1,
    int PageSize = 50,
    Guid? AssetId = null,
    MaintenanceOrderStatus? Status = null,
    /// <summary>openedAtUtc descendente (default) | folio | assetFolio | type | status</summary>
    string? SortBy = null,
    bool SortDescending = false)
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

        var joined =
            from o in query
            join asset in db.Assets.AsNoTracking() on o.AssetId equals asset.Id
            select new { Order = o, AssetFolio = asset.InternalFolio };

        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "folio" => descending ? joined.OrderByDescending(x => x.Order.Folio) : joined.OrderBy(x => x.Order.Folio),
            "assetFolio" => descending ? joined.OrderByDescending(x => x.AssetFolio) : joined.OrderBy(x => x.AssetFolio),
            "type" => descending ? joined.OrderByDescending(x => x.Order.Type) : joined.OrderBy(x => x.Order.Type),
            "status" => descending ? joined.OrderByDescending(x => x.Order.Status) : joined.OrderBy(x => x.Order.Status),
            "openedAtUtc" => descending
                ? joined.OrderByDescending(x => x.Order.OpenedAtUtc)
                : joined.OrderBy(x => x.Order.OpenedAtUtc),
            // Default histórico (sin SortBy): más reciente primero, sin importar SortDescending.
            _ => joined.OrderByDescending(x => x.Order.OpenedAtUtc),
        };

        var projected = ordered.Select(x => new MaintenanceOrderSummary(
            x.Order.Id, x.Order.AssetId, x.AssetFolio, x.Order.Folio, x.Order.Type, x.Order.Status,
            x.Order.OpenedAtUtc, x.Order.ClosedAtUtc));

        return PagedResult<MaintenanceOrderSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
