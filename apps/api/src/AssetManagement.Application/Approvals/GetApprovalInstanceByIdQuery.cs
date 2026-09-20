using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Approvals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Approvals;

public sealed record GetApprovalInstanceByIdQuery(Guid ApprovalInstanceId) : IRequest<ApprovalInstanceDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Approvals.Read;
}

public sealed record ApprovalStepInfo(Guid ApproverUserId, string ApproverDisplayName, string Decision, string? Comment, DateTimeOffset DecidedAtUtc);

public sealed record ApprovalInstanceDetail(
    Guid Id,
    string ContextType,
    Guid ContextId,
    Guid RequestedByUserId,
    string RequestedByDisplayName,
    string? Comment,
    ApprovalInstanceStatus Status,
    ApprovalMode Mode,
    int RequiredApprovals,
    IReadOnlyList<ApprovalStepInfo> Steps,
    DateTimeOffset CreatedAtUtc);

public sealed class GetApprovalInstanceByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetApprovalInstanceByIdQuery, ApprovalInstanceDetail>
{
    public async Task<ApprovalInstanceDetail> Handle(GetApprovalInstanceByIdQuery request, CancellationToken cancellationToken)
    {
        var instance = await db.ApprovalInstances.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.ApprovalInstanceId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApprovalInstance), request.ApprovalInstanceId);

        var userIds = instance.Steps.Select(s => s.ApproverUserId).Append(instance.RequestedByUserId).Distinct().ToList();
        var displayNamesById = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        return new ApprovalInstanceDetail(
            instance.Id, instance.ContextType, instance.ContextId, instance.RequestedByUserId,
            displayNamesById.GetValueOrDefault(instance.RequestedByUserId, "(usuario eliminado)"), instance.Comment,
            instance.Status, instance.Mode, instance.RequiredApprovals,
            instance.Steps
                .OrderBy(s => s.DecidedAtUtc)
                .Select(s => new ApprovalStepInfo(
                    s.ApproverUserId, displayNamesById.GetValueOrDefault(s.ApproverUserId, "(usuario eliminado)"),
                    s.Decision.ToString(), s.Comment, s.DecidedAtUtc))
                .ToList(),
            instance.CreatedAtUtc);
    }
}
