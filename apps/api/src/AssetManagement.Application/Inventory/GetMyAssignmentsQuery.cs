using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>Self-service: every assignment made to the caller, regardless of RBAC permission — same
/// self-scoping precedent as GetMeQuery. Pending ones are what "Mis asignaciones" asks the user to act
/// on; the rest is for their own reference.</summary>
public sealed record GetMyAssignmentsQuery : IRequest<IReadOnlyList<MyAssignmentSummary>>
{
}

public sealed record MyAssignmentSummary(
    Guid Id,
    Guid AssetId,
    string AssetFolio,
    string AssetBrand,
    string AssetModel,
    AssignmentStatus Status,
    DateTimeOffset AssignedAtUtc,
    Guid? GroupId);

public sealed class GetMyAssignmentsQueryHandler(IApplicationDbContext db, ICurrentUserContext currentUser)
    : IRequestHandler<GetMyAssignmentsQuery, IReadOnlyList<MyAssignmentSummary>>
{
    public async Task<IReadOnlyList<MyAssignmentSummary>> Handle(GetMyAssignmentsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión.");
        }

        var results =
            from a in db.Assignments.AsNoTracking()
            where a.AssignedToUserId == userId
            join asset in db.Assets.AsNoTracking() on a.AssetId equals asset.Id
            // Ordering directly by the boolean "a.Status == PendingSignature" makes SQL Server's
            // provider emit an invalid nvarchar '^' comparison for the string-converted enum — an
            // explicit CASE-shaped int key (0/1) translates cleanly instead.
            orderby a.Status == AssignmentStatus.PendingSignature ? 0 : 1, a.AssignedAtUtc descending
            select new MyAssignmentSummary(
                a.Id, a.AssetId, asset.InternalFolio, asset.Brand, asset.Model, a.Status, a.AssignedAtUtc,
                a.AssignmentGroupId);

        return await results.ToListAsync(cancellationToken);
    }
}
