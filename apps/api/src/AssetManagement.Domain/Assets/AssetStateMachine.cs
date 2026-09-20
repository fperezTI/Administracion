using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Assets;

/// <summary>
/// The valid transition graph for <see cref="AssetStatus"/>, exactly as documented in
/// docs/architecture/domain-model.md. Fixed in code by design (ADR-level decision already recorded
/// there): letting the graph itself be data would allow configuration to produce corrupt states. Which
/// transitions additionally require justification/evidence/approval is a later-phase, application-level
/// concern (F4 Approvals) — this class only answers "is this transition structurally legal at all".
/// </summary>
public static class AssetStateMachine
{
    private static readonly Dictionary<AssetStatus, AssetStatus[]> Transitions = new()
    {
        [AssetStatus.InWarehouse] =
        [
            AssetStatus.Reserved, AssetStatus.Assigned, AssetStatus.OnLoan, AssetStatus.InTransit,
            AssetStatus.InMaintenance, AssetStatus.PendingDecommission,
        ],
        [AssetStatus.Reserved] = [AssetStatus.InWarehouse, AssetStatus.Assigned, AssetStatus.OnLoan],
        [AssetStatus.Assigned] =
        [
            AssetStatus.InWarehouse, AssetStatus.OnLoan, AssetStatus.InTransit, AssetStatus.InMaintenance,
            AssetStatus.Damaged, AssetStatus.Lost, AssetStatus.Stolen, AssetStatus.PendingDecommission,
        ],
        [AssetStatus.OnLoan] =
        [
            AssetStatus.InWarehouse, AssetStatus.Assigned, AssetStatus.Damaged, AssetStatus.Lost,
            AssetStatus.Stolen,
        ],
        [AssetStatus.InTransit] = [AssetStatus.InWarehouse, AssetStatus.Assigned],
        [AssetStatus.InMaintenance] =
        [
            AssetStatus.InWarehouse, AssetStatus.UnderWarranty, AssetStatus.PendingDecommission,
            AssetStatus.Damaged,
        ],
        [AssetStatus.UnderWarranty] =
            [AssetStatus.InWarehouse, AssetStatus.InMaintenance, AssetStatus.PendingDecommission],
        [AssetStatus.Damaged] = [AssetStatus.InMaintenance, AssetStatus.PendingDecommission],
        [AssetStatus.Lost] = [AssetStatus.PendingDecommission],
        [AssetStatus.Stolen] = [AssetStatus.PendingDecommission],
        // InWarehouse: added in F4 — a rejected decommission request must return the asset to service
        // (see ADR 0006); F2's original graph had no way back from PendingDecommission.
        [AssetStatus.PendingDecommission] = [AssetStatus.Decommissioned, AssetStatus.InWarehouse],
        [AssetStatus.Decommissioned] = [AssetStatus.Sold, AssetStatus.Donated, AssetStatus.Destroyed],
        [AssetStatus.Sold] = [],
        [AssetStatus.Donated] = [],
        [AssetStatus.Destroyed] = [],
    };

    public static bool CanTransition(AssetStatus from, AssetStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static void EnsureCanTransition(AssetStatus from, AssetStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new DomainException($"No es válido transicionar un activo de '{from}' a '{to}'.");
        }
    }
}
