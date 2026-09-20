namespace AssetManagement.Domain.SparePartsAndConsumables;

/// <summary>One interval of a <see cref="SparePart"/> being installed in an asset — "historial de
/// instalación/retiro" (pedido). <see cref="RemovedAtUtc"/> is null while the interval is still open (the
/// part is currently installed).</summary>
public sealed class SparePartInstallation
{
    /// <summary>A real surrogate id, unlike <see cref="Maintenance.MaintenanceChecklistVersion"/>'s
    /// composite key — two installations of the same part could otherwise collide on
    /// <c>(SparePartId, InstalledAtUtc)</c> under a fake test clock.</summary>
    public Guid Id { get; private set; }

    public Guid SparePartId { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid? MaintenanceOrderId { get; private set; }
    public DateTimeOffset InstalledAtUtc { get; private set; }
    public DateTimeOffset? RemovedAtUtc { get; private set; }

    private SparePartInstallation()
    {
    }

    internal static SparePartInstallation Create(
        Guid sparePartId, Guid assetId, Guid? maintenanceOrderId, DateTimeOffset nowUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            SparePartId = sparePartId,
            AssetId = assetId,
            MaintenanceOrderId = maintenanceOrderId,
            InstalledAtUtc = nowUtc,
        };

    internal void Close(DateTimeOffset nowUtc) => RemovedAtUtc = nowUtc;
}
