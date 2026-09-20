using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Requests;

/// <summary>Self-service: every request the caller made, regardless of RBAC permission — same
/// self-scoping precedent as <c>GetMyAssignmentsQuery</c>.</summary>
public sealed record GetMyInternalRequestsQuery : IRequest<IReadOnlyList<MyInternalRequestSummary>>
{
}

public sealed record MyInternalRequestSummary(
    Guid Id, InternalRequestType Type, Guid AssetId, string AssetFolio, string Justification,
    DateOnly? ExpectedReturnDate, InternalRequestStatus Status, DateTimeOffset RequestedAtUtc);

public sealed class GetMyInternalRequestsQueryHandler(IApplicationDbContext db, ICurrentUserContext currentUser)
    : IRequestHandler<GetMyInternalRequestsQuery, IReadOnlyList<MyInternalRequestSummary>>
{
    public async Task<IReadOnlyList<MyInternalRequestSummary>> Handle(GetMyInternalRequestsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión.");
        }

        var results =
            from r in db.InternalRequests.AsNoTracking()
            where r.RequestedByUserId == userId
            join asset in db.Assets.AsNoTracking() on r.AssetId equals asset.Id
            orderby r.Status == InternalRequestStatus.PendingApproval ? 0 : 1, r.RequestedAtUtc descending
            select new MyInternalRequestSummary(
                r.Id, r.Type, r.AssetId, asset.InternalFolio, r.Justification, r.ExpectedReturnDate, r.Status, r.RequestedAtUtc);

        return await results.ToListAsync(cancellationToken);
    }
}
