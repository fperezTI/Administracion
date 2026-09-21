using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Assets;

/// <summary>
/// The core aggregate of the Asset Registry context (pedido §11). CompanyId is immutable here by
/// design — it only ever changes as the effect of a completed cross-company Transfer (a future phase),
/// never through a direct edit — see docs/multi-company.md.
/// </summary>
public sealed class Asset : AuditableAggregateRoot<Guid>
{
    private readonly List<AssetCustomFieldValue> _customFieldValues = [];

    public Guid CompanyId { get; private set; }
    public Guid AssetCategoryId { get; private set; }

    public string InternalFolio { get; private set; } = null!;
    public string? PatrimonialFolio { get; private set; }
    public string Brand { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public string? SerialNumber { get; private set; }
    public string? Description { get; private set; }

    public AssetStatus Status { get; private set; }
    public PhysicalCondition PhysicalCondition { get; private set; }
    public Guid? CurrentOrgUnitId { get; private set; }

    /// <summary>Optional link to another <see cref="Asset"/> this one is an accessory of (e.g. a charger
    /// for a laptop) — restricted to a single level by the commands that set it (an accessory can never
    /// itself have accessories), so this field alone is never enough to reconstruct a chain deeper than
    /// one hop. Assigning the primary asset cascades to every asset pointing here — see
    /// AssetManagement.Application.Inventory.AssignmentGroupSupport.</summary>
    public Guid? AccessoryOfAssetId { get; private set; }

    public DateOnly? AcquisitionDate { get; private set; }
    public decimal? AcquisitionCost { get; private set; }
    public string? Currency { get; private set; }
    public string? Supplier { get; private set; }
    public string? Invoice { get; private set; }
    public string? PurchaseOrder { get; private set; }

    public DateOnly? WarrantyStartDate { get; private set; }
    public DateOnly? WarrantyEndDate { get; private set; }
    public string? SupportContract { get; private set; }
    public string? SupportProvider { get; private set; }

    public AssetTag? Tag { get; private set; }

    public IReadOnlyCollection<AssetCustomFieldValue> CustomFieldValues => _customFieldValues.AsReadOnly();

    private Asset()
    {
    }

    private Asset(
        Guid id, Guid companyId, Guid assetCategoryId, string internalFolio, string brand, string model,
        string? serialNumber, string? description, PhysicalCondition condition, DateTimeOffset nowUtc,
        Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        CompanyId = companyId;
        AssetCategoryId = assetCategoryId;
        InternalFolio = internalFolio;
        Brand = brand;
        Model = model;
        SerialNumber = serialNumber;
        Description = description;
        PhysicalCondition = condition;
        Status = AssetStatus.InWarehouse;
    }

    public static Asset Create(
        Guid companyId, Guid assetCategoryId, string internalFolio, string brand, string model,
        string? serialNumber, string? description, PhysicalCondition condition, DateTimeOffset nowUtc,
        Guid? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(internalFolio))
        {
            throw new DomainException("El folio interno es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new DomainException("La marca del activo es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new DomainException("El modelo del activo es obligatorio.");
        }

        return new Asset(
            Guid.NewGuid(), companyId, assetCategoryId, internalFolio.Trim(), brand.Trim(), model.Trim(),
            serialNumber?.Trim(), description?.Trim(), condition, nowUtc, createdByUserId);
    }

    public void UpdateProfile(
        string brand, string model, string? serialNumber, string? description, string? patrimonialFolio,
        DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new DomainException("La marca del activo es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new DomainException("El modelo del activo es obligatorio.");
        }

        Brand = brand.Trim();
        Model = model.Trim();
        SerialNumber = serialNumber?.Trim();
        Description = description?.Trim();
        PatrimonialFolio = patrimonialFolio?.Trim();
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void UpdateFinancialInfo(
        DateOnly? acquisitionDate, decimal? acquisitionCost, string? currency, string? supplier,
        string? invoice, string? purchaseOrder, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        AcquisitionDate = acquisitionDate;
        AcquisitionCost = acquisitionCost;
        Currency = currency?.Trim().ToUpperInvariant();
        Supplier = supplier?.Trim();
        Invoice = invoice?.Trim();
        PurchaseOrder = purchaseOrder?.Trim();
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void UpdateContractualInfo(
        DateOnly? warrantyStartDate, DateOnly? warrantyEndDate, string? supportContract,
        string? supportProvider, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        WarrantyStartDate = warrantyStartDate;
        WarrantyEndDate = warrantyEndDate;
        SupportContract = supportContract?.Trim();
        SupportProvider = supportProvider?.Trim();
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void SetCondition(PhysicalCondition condition, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        PhysicalCondition = condition;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void MoveToOrgUnit(Guid? orgUnitId, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        CurrentOrgUnitId = orgUnitId;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    /// <summary>Chain-depth and cross-company validation live in the application command (they need to
    /// query other assets) — this guards only the one invariant the entity can check by itself.</summary>
    public void LinkAsAccessoryOf(Guid primaryAssetId, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (primaryAssetId == Id)
        {
            throw new DomainException("Un activo no puede ser accesorio de sí mismo.");
        }

        AccessoryOfAssetId = primaryAssetId;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void UnlinkAccessory(DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        AccessoryOfAssetId = null;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    /// <summary>The only way <see cref="Status"/> ever changes — every Inventory Operations command
    /// (F3) and later phases (F4 Approvals, F6 Maintenance) must go through this, never assign the
    /// property directly, so the state graph in <see cref="AssetStateMachine"/> can never be bypassed.</summary>
    public void ChangeStatus(AssetStatus newStatus, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        AssetStateMachine.EnsureCanTransition(Status, newStatus);
        Status = newStatus;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    /// <summary>The only place <see cref="CompanyId"/> can ever change (CLAUDE.md regla 3, F5) — called
    /// exclusively by a completed cross-company <see cref="Inventory.Transfer"/> receipt, never directly.
    /// Regenerates <see cref="InternalFolio"/> in the destination company's own sequence rather than
    /// keeping the old one: a folio is unique only per company (ADR 0004), so carrying it over verbatim
    /// could collide with an unrelated asset the destination company already folioed independently — see
    /// ADR 0007. <see cref="Tag"/>'s globally-unique <c>Code</c> is deliberately left untouched: it is the
    /// one identity that survives the move (exactly what ADR 0004 was designed for). Clears
    /// <see cref="CurrentOrgUnitId"/> because an OrgUnit belongs to a specific company — the old one is no
    /// longer valid; F5 leaves relocating into the new company's structure to the existing
    /// RelocateAssetCommand rather than asking for it here too.</summary>
    public void CompleteCrossCompanyTransfer(
        Guid newCompanyId, string newInternalFolio, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(newInternalFolio))
        {
            throw new DomainException("El nuevo folio interno es obligatorio.");
        }

        AssetStateMachine.EnsureCanTransition(Status, AssetStatus.InWarehouse);

        CompanyId = newCompanyId;
        InternalFolio = newInternalFolio.Trim();
        CurrentOrgUnitId = null;
        Status = AssetStatus.InWarehouse;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void SetCustomFieldValues(IEnumerable<(Guid FieldId, string Value)> values)
    {
        _customFieldValues.Clear();
        foreach (var (fieldId, value) in values)
        {
            _customFieldValues.Add(AssetCustomFieldValue.Create(Id, fieldId, value));
        }
    }

    public AssetTag IssueTag(string code, IdentificationTechnology technology, DateTimeOffset nowUtc)
    {
        if (Tag is not null)
        {
            throw new DomainException("Este activo ya tiene una etiqueta emitida.");
        }

        Tag = AssetTag.Issue(Id, code, technology, nowUtc);
        return Tag;
    }

    public void ReprintTag(DateTimeOffset nowUtc)
    {
        if (Tag is null)
        {
            throw new DomainException("Este activo todavía no tiene una etiqueta emitida.");
        }

        Tag.RecordReprint(nowUtc);
    }
}
