namespace AssetManagement.Domain.Maintenance;

/// <summary>One checked-off item of the checklist version snapshotted onto a <see cref="MaintenanceOrder"/>
/// at close time. <see cref="ItemText"/> is a copy, not a live reference, so it stays accurate even if the
/// checklist definition changes or is deactivated later — same snapshot rationale as
/// <see cref="Approvals.ApprovalInstance"/>'s captured approver roles.</summary>
public sealed class MaintenanceOrderChecklistResult
{
    public Guid MaintenanceOrderId { get; private set; }
    public int ItemIndex { get; private set; }
    public string ItemText { get; private set; } = null!;
    public bool IsCompleted { get; private set; }
    public string? Notes { get; private set; }

    private MaintenanceOrderChecklistResult()
    {
    }

    internal static MaintenanceOrderChecklistResult Create(
        Guid maintenanceOrderId, int itemIndex, string itemText, bool isCompleted, string? notes) =>
        new()
        {
            MaintenanceOrderId = maintenanceOrderId,
            ItemIndex = itemIndex,
            ItemText = itemText,
            IsCompleted = isCompleted,
            Notes = notes?.Trim(),
        };

    /// <summary>Fills in the technician's result for this item — called only from
    /// <see cref="MaintenanceOrder.Close"/>, once, while the order is still open.</summary>
    internal void RecordResult(bool isCompleted, string? notes)
    {
        IsCompleted = isCompleted;
        Notes = notes?.Trim();
    }
}
