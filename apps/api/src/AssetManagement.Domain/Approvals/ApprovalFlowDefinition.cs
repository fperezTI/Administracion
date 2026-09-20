using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Approvals;

/// <summary>
/// Configuration for a type of approval (pedido §10) — e.g. "asset.decommission". Approvers are a set of
/// <em>roles</em>, not fixed people: membership is dynamic, resolved at decision time against whoever
/// currently holds the role and has access to the company (same principle as every other RBAC check in
/// this system). Never seeded by migration — unlike <c>PermissionCatalog</c>, a flow references real
/// <c>RoleId</c>s that only exist once an administrator creates them in their own tenant; see ADR 0006.
/// <see cref="ApproverRoleIds"/> order only matters when <see cref="Mode"/> is <see cref="ApprovalMode.Sequential"/>.
/// Editing an existing flow's rules never happens — deactivate and create a new one, so in-flight
/// <see cref="ApprovalInstance"/>s (which snapshot the rules at creation) are never retroactively affected.
/// </summary>
public sealed class ApprovalFlowDefinition : AuditableAggregateRoot<Guid>
{
    public string Key { get; private set; } = null!;
    public Guid? CompanyId { get; private set; }

    /// <summary>Mapped by EF Core as a primitive collection (native support since EF Core 8) — a plain
    /// settable property, not a navigation, so it needs none of the backing-field/owned-collection
    /// machinery that <see cref="ApprovalInstance.Steps"/> uses.</summary>
    public IReadOnlyList<Guid> ApproverRoleIds { get; private set; } = [];

    public int RequiredApprovals { get; private set; }
    public ApprovalMode Mode { get; private set; }
    public bool RequiresComment { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ApprovalFlowDefinition()
    {
    }

    private ApprovalFlowDefinition(
        Guid id, string key, Guid? companyId, IEnumerable<Guid> approverRoleIds, int requiredApprovals,
        ApprovalMode mode, bool requiresComment, DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        Key = key;
        CompanyId = companyId;
        ApproverRoleIds = approverRoleIds.ToList();
        RequiredApprovals = requiredApprovals;
        Mode = mode;
        RequiresComment = requiresComment;
    }

    public static ApprovalFlowDefinition Create(
        string key, Guid? companyId, IReadOnlyList<Guid> approverRoleIds, int requiredApprovals,
        ApprovalMode mode, bool requiresComment, DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("La clave del flujo de aprobación es obligatoria.");
        }

        if (approverRoleIds.Count == 0)
        {
            throw new DomainException("Un flujo de aprobación necesita al menos un rol aprobador.");
        }

        if (requiredApprovals < 1)
        {
            throw new DomainException("El número de aprobaciones requeridas debe ser al menos 1.");
        }

        if (mode == ApprovalMode.Sequential && requiredApprovals != approverRoleIds.Count)
        {
            throw new DomainException("En modo secuencial se requiere una aprobación por cada rol, en orden.");
        }

        // Parallel mode has no upper bound tied to the role list's length: several different people can
        // hold the same role, so e.g. roles=[Manager], requiredApprovals=3 legitimately asks for three
        // distinct managers to each approve — the list names *which* roles count, not how many decisions
        // are possible.

        return new ApprovalFlowDefinition(
            Guid.NewGuid(), key.Trim(), companyId, approverRoleIds, requiredApprovals, mode, requiresComment,
            nowUtc, createdByUserId);
    }

    public void Deactivate(DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        IsActive = false;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void Activate(DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        IsActive = true;
        RecordUpdate(nowUtc, updatedByUserId);
    }
}
