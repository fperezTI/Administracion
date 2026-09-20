using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Approvals.Events;

/// <summary>
/// Raised when an <see cref="ApprovalInstance"/> is created, so eligible approvers can be notified they
/// have a pending decision (F8's notification center) — added retroactively to F4's <c>Create</c>, purely
/// additive. V1 simplification: notifies everyone holding any of <see cref="ApproverRoleIds"/> regardless
/// of <see cref="ApprovalMode"/> turn order, rather than recomputing whose turn it is (see F8 plan).
/// </summary>
public sealed record ApprovalRequested(
    Guid ApprovalInstanceId, string ContextType, Guid ContextId, Guid CompanyId, Guid RequestedByUserId,
    IReadOnlyList<Guid> ApproverRoleIds, DateTimeOffset OccurredOnUtc)
    : IDomainEvent;
