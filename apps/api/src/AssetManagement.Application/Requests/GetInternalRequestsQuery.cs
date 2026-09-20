using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Requests;

public sealed record GetInternalRequestsQuery(
    Guid CompanyId, int PageNumber = 1, int PageSize = 50, InternalRequestStatus? Status = null)
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

        var projected =
            from r in query
            join asset in db.Assets.AsNoTracking() on r.AssetId equals asset.Id
            join requester in db.Users.AsNoTracking() on r.RequestedByUserId equals requester.Id
            orderby r.RequestedAtUtc descending
            select new InternalRequestSummary(
                r.Id, r.Type, r.AssetId, asset.InternalFolio, r.RequestedByUserId, requester.DisplayName, r.Status, r.RequestedAtUtc);

        return PagedResult<InternalRequestSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
