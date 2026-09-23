using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Requests;

public sealed record GetInternalRequestsQuery(
    Guid CompanyId,
    int PageNumber = 1,
    int PageSize = 50,
    InternalRequestStatus? Status = null,
    /// <summary>requestedAtUtc descendente (default) | assetFolio | type | requestedByDisplayName | status</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<InternalRequestSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Requests.Read;
}

public sealed record InternalRequestSummary(
    Guid Id, InternalRequestType Type, Guid AssetId, string AssetFolio, Guid RequestedByUserId,
    string RequestedByDisplayName, InternalRequestStatus Status, DateTimeOffset RequestedAtUtc);

public sealed class GetInternalRequestsQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetInternalRequestsQuery, PagedResult<InternalRequestSummary>>
{
    public Task<PagedResult<InternalRequestSummary>> Handle(GetInternalRequestsQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var query = db.InternalRequests.AsNoTracking().Where(r => r.CompanyId == request.CompanyId);

        if (request.Status is { } status)
        {
            query = query.Where(r => r.Status == status);
        }

        var joined =
            from r in query
            join asset in db.Assets.AsNoTracking() on r.AssetId equals asset.Id
            join requester in db.Users.AsNoTracking() on r.RequestedByUserId equals requester.Id
            select new { Request = r, AssetFolio = asset.InternalFolio, RequesterName = requester.DisplayName };

        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "assetFolio" => descending ? joined.OrderByDescending(x => x.AssetFolio) : joined.OrderBy(x => x.AssetFolio),
            "type" => descending ? joined.OrderByDescending(x => x.Request.Type) : joined.OrderBy(x => x.Request.Type),
            "requestedByDisplayName" => descending
                ? joined.OrderByDescending(x => x.RequesterName)
                : joined.OrderBy(x => x.RequesterName),
            "status" => descending ? joined.OrderByDescending(x => x.Request.Status) : joined.OrderBy(x => x.Request.Status),
            "requestedAtUtc" => descending
                ? joined.OrderByDescending(x => x.Request.RequestedAtUtc)
                : joined.OrderBy(x => x.Request.RequestedAtUtc),
            // Default histórico (sin SortBy): más reciente primero, sin importar SortDescending.
            _ => joined.OrderByDescending(x => x.Request.RequestedAtUtc),
        };

        var projected = ordered.Select(x => new InternalRequestSummary(
            x.Request.Id, x.Request.Type, x.Request.AssetId, x.AssetFolio, x.Request.RequestedByUserId,
            x.RequesterName, x.Request.Status, x.Request.RequestedAtUtc));

        return PagedResult<InternalRequestSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
