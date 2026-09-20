using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.SparePartsAndConsumables;

/// <summary>
/// A consumable type tracked "por existencia" (pedido) — unlike <see cref="SparePart"/>, individual units
/// are not identified; only the running quantity matters. <see cref="CurrentStock"/> is private-set and
/// changes only through <see cref="ApplyStockMovement"/>, mirroring `docs/architecture/domain-model.md`:
/// "Existencia solo cambia vía ConsumableStockMovement".
/// </summary>
public sealed class Consumable : AuditableAggregateRoot<Guid>
{
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Sku { get; private set; }
    public string UnitOfMeasure { get; private set; } = null!;
    public decimal? MinimumStock { get; private set; }
    public decimal CurrentStock { get; private set; }

    private Consumable()
    {
    }

    private Consumable(
        Guid id, Guid companyId, string name, string? sku, string unitOfMeasure, decimal? minimumStock,
        DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        CompanyId = companyId;
        Name = name;
        Sku = sku;
        UnitOfMeasure = unitOfMeasure;
        MinimumStock = minimumStock;
        CurrentStock = 0;
    }

    public static Consumable Create(
        Guid companyId, string name, string? sku, string unitOfMeasure, decimal? minimumStock,
        DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        Validate(name, unitOfMeasure, minimumStock);

        return new Consumable(Guid.NewGuid(), companyId, name.Trim(), sku?.Trim(), unitOfMeasure.Trim(), minimumStock, nowUtc, createdByUserId);
    }

    public void UpdateProfile(
        string name, string? sku, string unitOfMeasure, decimal? minimumStock, DateTimeOffset nowUtc,
        Guid? updatedByUserId)
    {
        Validate(name, unitOfMeasure, minimumStock);

        Name = name.Trim();
        Sku = sku?.Trim();
        UnitOfMeasure = unitOfMeasure.Trim();
        MinimumStock = minimumStock;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    /// <summary>The only way <see cref="CurrentStock"/> ever changes. Called alongside creating the
    /// corresponding immutable <see cref="ConsumableStockMovement"/> row (Application layer) — this method
    /// only protects the "never negative" invariant, it knows nothing about the movement record itself.</summary>
    public void ApplyStockMovement(ConsumableStockDirection direction, decimal quantity, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (quantity <= 0)
        {
            throw new DomainException("La cantidad del movimiento de existencia debe ser mayor a cero.");
        }

        var newStock = direction == ConsumableStockDirection.In ? CurrentStock + quantity : CurrentStock - quantity;
        if (newStock < 0)
        {
            throw new DomainException("Este movimiento dejaría la existencia del consumible en negativo.");
        }

        CurrentStock = newStock;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    private static void Validate(string name, string unitOfMeasure, decimal? minimumStock)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("El nombre del consumible es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(unitOfMeasure))
        {
            throw new DomainException("La unidad de medida del consumible es obligatoria.");
        }

        if (minimumStock is < 0)
        {
            throw new DomainException("La existencia mínima no puede ser negativa.");
        }
    }
}
