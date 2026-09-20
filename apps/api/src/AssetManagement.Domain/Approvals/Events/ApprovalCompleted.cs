using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Approvals.Events;

/// <summary>
/// Raised when an <see cref="ApprovalInstance"/> reaches its required approval count. The module that
/// requested the approval (e.g. Inventory Operations) reacts to this — the Approvals context itself
/// never imports assemblies from any other context (pedido §10).
/// </summary>
public sealed record ApprovalCompleted(Guid ApprovalInstanceId, string ContextType, Guid ContextId, DateTimeOffset OccurredOnUtc)
    : IDomainEvent;
