using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.SparePartsAndConsumables;

/// <summary>
/// A serialized spare part (pedido: "refacciones serializadas") — unlike a <see cref="Consumable"/>, it is
/// tracked as one physical, individually identified unit, not by existence/quantity. Lives at a warehouse
/// <c>OrgUnit</c> while <see cref="SparePartStatus.InStock"/>, or inside an asset while
/// <see cref="SparePartStatus.Installed"/>; <see cref="_installations"/> is its full install/removal
/// history (pedido: "historial de instalación/retiro").
/// </summary>
public sealed class SparePart : AuditableAggregateRoot<Guid>
{
    private readonly List<SparePartInstallation> _installations = [];

    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? PartNumber { get; private set; }
    public string SerialNumber { get; private set; } = null!;
    public SparePartStatus Status { get; private set; }
    public Guid? CurrentAssetId { get; private set; }
    public Guid? CurrentWarehouseOrgUnitId { get; private set; }

    public IReadOnlyCollection<SparePartInstallation> Installations => _installations.AsReadOnly();

    private SparePart()
    {
    }

    private SparePart(
        Guid id, Guid companyId, string name, string? partNumber, string serialNumber, Guid warehouseOrgUnitId,
        DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        CompanyId = companyId;
        Name = name;
        PartNumber = partNumber;
        SerialNumber = serialNumber;
        Status = SparePartStatus.InStock;
        CurrentWarehouseOrgUnitId = warehouseOrgUnitId;
    }

    public static SparePart Create(
        Guid companyId, string name, string? partNumber, string serialNumber, Guid warehouseOrgUnitId,
        DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("El nombre de la refacción es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            throw new DomainException("El número de serie de la refacción es obligatorio.");
        }

        return new SparePart(
            Guid.NewGuid(), companyId, name.Trim(), partNumber?.Trim(), serialNumber.Trim(), warehouseOrgUnitId,
            nowUtc, createdByUserId);
    }

    public void Install(Guid assetId, Guid? maintenanceOrderId, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != SparePartStatus.InStock)
        {
            throw new DomainException("Solo una refacción en existencia puede instalarse.");
        }

        _installations.Add(SparePartInstallation.Create(Id, assetId, maintenanceOrderId, nowUtc));
        Status = SparePartStatus.Installed;
        CurrentAssetId = assetId;
        CurrentWarehouseOrgUnitId = null;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void Uninstall(Guid warehouseOrgUnitId, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != SparePartStatus.Installed)
        {
            throw new DomainException("Solo una refacción instalada puede retirarse.");
        }

        var openInstallation = _installations.SingleOrDefault(i => i.RemovedAtUtc is null)
            ?? throw new DomainException("No se encontró el registro de instalación vigente de esta refacción.");
        openInstallation.Close(nowUtc);

        Status = SparePartStatus.InStock;
        CurrentAssetId = null;
        CurrentWarehouseOrgUnitId = warehouseOrgUnitId;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    /// <summary>Named <c>MarkDisposed</c> rather than <c>Dispose</c> to avoid colliding with
    /// <see cref="IDisposable"/>'s well-known meaning — this retires the physical part, it does not
    /// release managed resources.</summary>
    public void MarkDisposed(DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != SparePartStatus.InStock)
        {
            throw new DomainException("Solo una refacción en existencia puede darse de baja (retírala primero si está instalada).");
        }

        Status = SparePartStatus.Disposed;
        CurrentWarehouseOrgUnitId = null;
        RecordUpdate(nowUtc, updatedByUserId);
    }
}
