namespace AssetManagement.Domain.Maintenance;

/// <summary>One immutable revision of a <see cref="MaintenanceChecklistDefinition"/>'s items — same
/// immutable-version rationale as <see cref="Templates.TemplateVersion"/>: a version already snapshotted
/// onto a closed <see cref="MaintenanceOrder"/> must never change under it.</summary>
public sealed class MaintenanceChecklistVersion
{
    public Guid ChecklistDefinitionId { get; private set; }
    public int VersionNumber { get; private set; }

    /// <summary>Primitive collection (native EF Core 8+ support) — same pattern as
    /// <see cref="Approvals.ApprovalFlowDefinition.ApproverRoleIds"/>.</summary>
    public IReadOnlyList<string> Items { get; private set; } = [];

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    private MaintenanceChecklistVersion()
    {
    }

    internal static MaintenanceChecklistVersion Create(
        Guid checklistDefinitionId, int versionNumber, IReadOnlyList<string> items, DateTimeOffset nowUtc,
        Guid? createdByUserId) =>
        new()
        {
            ChecklistDefinitionId = checklistDefinitionId,
            VersionNumber = versionNumber,
            Items = items,
            CreatedAtUtc = nowUtc,
            CreatedByUserId = createdByUserId,
        };
}
