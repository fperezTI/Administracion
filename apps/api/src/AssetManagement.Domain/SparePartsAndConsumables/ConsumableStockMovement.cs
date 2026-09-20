using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.SparePartsAndConsumables;

/// <summary>
/// The immutable audit trail of one change in a <see cref="Consumable"/>'s existence (pedido: "todo cambio
/// de existencia genera un movimiento de inventario de consumible") — own aggregate, independent of
/// <see cref="Consumable"/>, exactly like <see cref="Inventory.Movement"/> is independent of
/// <see cref="Assets.Asset"/>: it must stay queryable as a full history without loading the (potentially
/// large) consumable's navigation collection.
/// </summary>
public sealed class ConsumableStockMovement : AuditableAggregateRoot<Guid>
{
    public Guid CompanyId { get; private set; }
    public Guid ConsumableId { get; private set; }
    public string Folio { get; private set; } = null!;
    public Guid WarehouseOrgUnitId { get; private set; }
    public ConsumableStockDirection Direction { get; private set; }
    public ConsumableStockMovementReason Reason { get; private set; }
    public decimal Quantity { get; private set; }
    public Guid? ReferenceMaintenanceOrderId { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    private ConsumableStockMovement()
    {
    }

    private ConsumableStockMovement(
        Guid id, Guid companyId, Guid consumableId, string folio, Guid warehouseOrgUnitId,
        ConsumableStockDirection direction, ConsumableStockMovementReason reason, decimal quantity,
        Guid? referenceMaintenanceOrderId, string? notes, DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        CompanyId = companyId;
        ConsumableId = consumableId;
        Folio = folio;
        WarehouseOrgUnitId = warehouseOrgUnitId;
        Direction = direction;
        Reason = reason;
        Quantity = quantity;
        ReferenceMaintenanceOrderId = referenceMaintenanceOrderId;
        Notes = notes;
        OccurredAtUtc = nowUtc;
    }

    public static ConsumableStockMovement Create(
        Guid companyId, Guid consumableId, string folio, Guid warehouseOrgUnitId, ConsumableStockDirection direction,
        ConsumableStockMovementReason reason, decimal quantity, Guid? referenceMaintenanceOrderId, string? notes,
        DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(folio))
        {
            throw new DomainException("El folio del movimiento de consumible es obligatorio.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("La cantidad del movimiento de existencia debe ser mayor a cero.");
        }

        return new ConsumableStockMovement(
            Guid.NewGuid(), companyId, consumableId, folio.Trim(), warehouseOrgUnitId, direction, reason, quantity,
            referenceMaintenanceOrderId, notes?.Trim(), nowUtc, createdByUserId);
    }
}
