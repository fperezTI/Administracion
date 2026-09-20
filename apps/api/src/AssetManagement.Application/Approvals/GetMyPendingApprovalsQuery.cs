using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Approvals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Approvals;

/// <summary>Self-service: pending approvals the caller is currently eligible to decide — same
/// self-scoping precedent as GetMeQuery/GetMyAssignmentsQuery (F3), filtered by role membership instead
/// of by being a named recipient. Pulls the (bounded, per-company) set of pending instances into memory
/// to evaluate sequential-mode "whose turn is it" logic, which doesn't translate cleanly into SQL over an
/// owned collection of role ids.</summary>
public sealed record GetMyPendingApprovalsQuery : IRequest<IReadOnlyList<MyPendingApprovalSummary>>
{
}

public sealed record MyPendingApprovalSummary(
    Guid Id, string ContextType, Guid ContextId, string RequestedByDisplayName, string? Comment, DateTimeOffset CreatedAtUtc);

public sealed class GetMyPendingApprovalsQueryHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetMyPendingApprovalsQuery, IReadOnlyList<MyPendingApprovalSummary>>
{
    public async Task<IReadOnlyList<MyPendingApprovalSummary>> Handle(
        GetMyPendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión.");
        }

        var myRoleIds = await db.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToListAsync(cancellationToken);
        if (myRoleIds.Count == 0)
        {
            return [];
        }

        var candidates = await db.ApprovalInstances.AsNoTracking()
            .Where(i => i.Status == ApprovalInstanceStatus.Pending
                && currentCompany.AccessibleCompanyIds.Contains(i.CompanyId)
                && i.RequestedByUserId != userId)
            .ToListAsync(cancellationToken);

        var eligible = candidates.Where(i => IsMyTurn(i, userId, myRoleIds)).ToList();
        if (eligible.Count == 0)
        {
            return [];
        }

        var requesterIds = eligible.Select(i => i.RequestedByUserId).Distinct().ToList();
        var displayNamesById = await db.Users.AsNoTracking()
            .Where(u => requesterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        return eligible
            .OrderBy(i => i.CreatedAtUtc)
            .Select(i => new MyPendingApprovalSummary(
                i.Id, i.ContextType, i.ContextId, displayNamesById.GetValueOrDefault(i.RequestedByUserId, "(usuario eliminado)"),
                i.Comment, i.CreatedAtUtc))
            .ToList();
    }

    private static bool IsMyTurn(ApprovalInstance instance, Guid userId, IReadOnlyCollection<Guid> myRoleIds)
    {
        if (instance.Steps.Any(s => s.ApproverUserId == userId))
        {
            return false;
        }

        if (instance.Mode == ApprovalMode.Parallel)
        {
            return instance.ApproverRoleIds.Any(myRoleIds.Contains);
        }

        var currentStepIndex = instance.Steps.Count(s => s.Decision == ApprovalStepDecision.Approved);
        if (currentStepIndex >= instance.ApproverRoleIds.Count)
        {
            return false;
        }

        return myRoleIds.Contains(instance.ApproverRoleIds[currentStepIndex]);
    }
}
