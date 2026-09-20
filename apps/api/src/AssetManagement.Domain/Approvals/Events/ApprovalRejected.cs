using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Approvals.Events;

public sealed record ApprovalRejected(
    Guid ApprovalInstanceId, string ContextType, Guid ContextId, string? Reason, DateTimeOffset OccurredOnUtc)
    : IDomainEvent;
