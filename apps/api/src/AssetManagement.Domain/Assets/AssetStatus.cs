namespace AssetManagement.Domain.Assets;

/// <summary>Lifecycle states from pedido §13. The transition graph lives in code
/// (<see cref="AssetStateMachine"/>), not configuration — see docs/architecture/domain-model.md.</summary>
public enum AssetStatus
{
    InWarehouse,
    Reserved,
    Assigned,
    OnLoan,
    InTransit,
    InMaintenance,
    UnderWarranty,
    Damaged,
    Lost,
    Stolen,
    PendingDecommission,
    Decommissioned,
    Sold,
    Donated,
    Destroyed,
}
