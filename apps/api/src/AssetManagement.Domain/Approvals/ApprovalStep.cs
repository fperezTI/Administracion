namespace AssetManagement.Domain.Approvals;

/// <summary>One decision recorded against an <see cref="ApprovalInstance"/>. Steps are created as
/// decisions happen — never pre-allocated placeholders for who "should" decide, since eligibility is a
/// dynamic role pool, not a fixed roster.</summary>
public sealed class ApprovalStep
{
    public Guid ApprovalInstanceId { get; private set; }
    public Guid ApproverUserId { get; private set; }
    public ApprovalStepDecision Decision { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset DecidedAtUtc { get; private set; }

    private ApprovalStep()
    {
    }

    internal static ApprovalStep Create(
        Guid approvalInstanceId, Guid approverUserId, ApprovalStepDecision decision, string? comment,
        DateTimeOffset nowUtc) =>
        new()
        {
            ApprovalInstanceId = approvalInstanceId,
            ApproverUserId = approverUserId,
            Decision = decision,
            Comment = comment?.Trim(),
            DecidedAtUtc = nowUtc,
        };
}
