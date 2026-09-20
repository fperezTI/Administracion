using AssetManagement.Domain.Approvals.Events;
using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Approvals;

/// <summary>
/// One concrete run of an <see cref="ApprovalFlowDefinition"/>, linked to the business operation that
/// requested it via a polymorphic reference (<see cref="ContextType"/>/<see cref="ContextId"/> — same
/// pattern as <see cref="Signature.SignatureRecord"/>). Snapshots the flow's rules at creation time so
/// editing (deactivating/recreating) the definition later never changes an in-flight instance. This
/// aggregate has zero knowledge of what it is approving — the requesting module (e.g. Inventory
/// Operations) reacts to <see cref="Events.ApprovalCompleted"/>/<see cref="Events.ApprovalRejected"/>
/// instead (pedido §10).
/// </summary>
public sealed class ApprovalInstance : AuditableAggregateRoot<Guid>
{
    private readonly List<ApprovalStep> _steps = [];

    public Guid FlowDefinitionId { get; private set; }
    public Guid CompanyId { get; private set; }
    public string ContextType { get; private set; } = null!;
    public Guid ContextId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public ApprovalInstanceStatus Status { get; private set; }

    /// <summary>Primitive collection (native EF Core 8+ support), snapshotted from the flow definition at
    /// creation — see <see cref="ApprovalFlowDefinition.ApproverRoleIds"/> for why this needs no
    /// backing-field navigation machinery.</summary>
    public IReadOnlyList<Guid> ApproverRoleIds { get; private set; } = [];

    public int RequiredApprovals { get; private set; }
    public ApprovalMode Mode { get; private set; }
    public string? Comment { get; private set; }
    public IReadOnlyCollection<ApprovalStep> Steps => _steps.AsReadOnly();

    private ApprovalInstance()
    {
    }

    private ApprovalInstance(
        Guid id, Guid flowDefinitionId, Guid companyId, string contextType, Guid contextId,
        Guid requestedByUserId, IEnumerable<Guid> approverRoleIds, int requiredApprovals, ApprovalMode mode,
        string? comment, DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        FlowDefinitionId = flowDefinitionId;
        CompanyId = companyId;
        ContextType = contextType;
        ContextId = contextId;
        RequestedByUserId = requestedByUserId;
        Status = ApprovalInstanceStatus.Pending;
        ApproverRoleIds = approverRoleIds.ToList();
        RequiredApprovals = requiredApprovals;
        Mode = mode;
        Comment = comment?.Trim();
    }

    public static ApprovalInstance Create(
        ApprovalFlowDefinition flowDefinition, Guid companyId, string contextType, Guid contextId,
        Guid requestedByUserId, string? comment, DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(contextType))
        {
            throw new DomainException("El tipo de contexto de la aprobación es obligatorio.");
        }

        if (flowDefinition.RequiresComment && string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainException("Este flujo de aprobación exige una justificación.");
        }

        var instance = new ApprovalInstance(
            Guid.NewGuid(), flowDefinition.Id, companyId, contextType, contextId, requestedByUserId,
            flowDefinition.ApproverRoleIds, flowDefinition.RequiredApprovals, flowDefinition.Mode, comment,
            nowUtc, createdByUserId);

        instance.Raise(new ApprovalRequested(
            instance.Id, contextType, contextId, companyId, requestedByUserId, flowDefinition.ApproverRoleIds, nowUtc));

        return instance;
    }

    /// <summary>
    /// Records one decision. <paramref name="deciderRoleIds"/> is the set of roles the caller holds that
    /// also appear in <see cref="ApproverRoleIds"/> — resolved by the Application handler (which has
    /// database access), never queried here: this aggregate stays free of persistence concerns.
    /// </summary>
    public void Decide(
        Guid deciderUserId, IReadOnlyCollection<Guid> deciderRoleIds, ApprovalStepDecision decision,
        string? comment, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != ApprovalInstanceStatus.Pending)
        {
            throw new DomainException("Esta aprobación ya no está pendiente.");
        }

        if (deciderUserId == RequestedByUserId)
        {
            throw new DomainException("Quien solicita una aprobación no puede decidir sobre su propia solicitud.");
        }

        if (_steps.Any(s => s.ApproverUserId == deciderUserId))
        {
            throw new DomainException("Ya registraste una decisión para esta aprobación.");
        }

        if (decision == ApprovalStepDecision.Rejected && string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainException("Rechazar una aprobación requiere indicar el motivo.");
        }

        var eligibleRoleIds = deciderRoleIds.Where(ApproverRoleIds.Contains).ToList();
        if (eligibleRoleIds.Count == 0)
        {
            throw new DomainException("No tienes un rol elegible para decidir sobre esta aprobación.");
        }

        if (Mode == ApprovalMode.Sequential)
        {
            var currentStepIndex = _steps.Count(s => s.Decision == ApprovalStepDecision.Approved);
            var roleForCurrentStep = ApproverRoleIds[currentStepIndex];
            if (!eligibleRoleIds.Contains(roleForCurrentStep))
            {
                throw new DomainException("Todavía no es el turno del rol correspondiente en este flujo secuencial.");
            }
        }

        _steps.Add(ApprovalStep.Create(Id, deciderUserId, decision, comment, nowUtc));

        if (decision == ApprovalStepDecision.Rejected)
        {
            Status = ApprovalInstanceStatus.Rejected;
            Raise(new ApprovalRejected(Id, ContextType, ContextId, comment, nowUtc));
        }
        else if (_steps.Count(s => s.Decision == ApprovalStepDecision.Approved) >= RequiredApprovals)
        {
            Status = ApprovalInstanceStatus.Approved;
            Raise(new ApprovalCompleted(Id, ContextType, ContextId, nowUtc));
        }

        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void Cancel(Guid callerUserId, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != ApprovalInstanceStatus.Pending)
        {
            throw new DomainException("Solo una aprobación pendiente puede cancelarse.");
        }

        if (callerUserId != RequestedByUserId)
        {
            throw new DomainException("Solo quien solicitó la aprobación puede cancelarla.");
        }

        Status = ApprovalInstanceStatus.Cancelled;
        RecordUpdate(nowUtc, updatedByUserId);
    }
}
