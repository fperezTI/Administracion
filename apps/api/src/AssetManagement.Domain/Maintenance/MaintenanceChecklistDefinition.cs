using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Maintenance;

/// <summary>A named, versioned maintenance checklist (pedido: "checklists versionados"), optionally scoped
/// to an <see cref="Assets.AssetCategory"/> (e.g. a laptop preventive-maintenance checklist). Linking one to
/// a <see cref="MaintenanceOrder"/> is optional — many corrective orders are ad-hoc. Same
/// create/version/deactivate shape as <see cref="Templates.Template"/>.</summary>
public sealed class MaintenanceChecklistDefinition : AuditableAggregateRoot<Guid>
{
    private readonly List<MaintenanceChecklistVersion> _versions = [];

    public string Key { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid? AssetCategoryId { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<MaintenanceChecklistVersion> Versions => _versions.AsReadOnly();
    public MaintenanceChecklistVersion? LatestVersion => _versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

    private MaintenanceChecklistDefinition()
    {
    }

    private MaintenanceChecklistDefinition(
        Guid id, string key, string name, Guid? assetCategoryId, DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        Key = key;
        Name = name;
        AssetCategoryId = assetCategoryId;
    }

    public static MaintenanceChecklistDefinition Create(
        string key, string name, Guid? assetCategoryId, IReadOnlyList<string> initialItems, DateTimeOffset nowUtc,
        Guid? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("La clave del checklist es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("El nombre del checklist es obligatorio.");
        }

        var checklist = new MaintenanceChecklistDefinition(Guid.NewGuid(), key.Trim(), name.Trim(), assetCategoryId, nowUtc, createdByUserId);
        checklist.AddVersion(initialItems, nowUtc, createdByUserId);
        return checklist;
    }

    public MaintenanceChecklistVersion AddVersion(IReadOnlyList<string> items, DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        if (items is not { Count: > 0 } || items.Any(string.IsNullOrWhiteSpace))
        {
            throw new DomainException("El checklist debe tener al menos un ítem, y ninguno puede estar vacío.");
        }

        var nextVersionNumber = (LatestVersion?.VersionNumber ?? 0) + 1;
        var version = MaintenanceChecklistVersion.Create(
            Id, nextVersionNumber, items.Select(i => i.Trim()).ToList(), nowUtc, createdByUserId);
        _versions.Add(version);
        RecordUpdate(nowUtc, createdByUserId);
        return version;
    }

    public void SetActive(bool isActive, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        IsActive = isActive;
        RecordUpdate(nowUtc, updatedByUserId);
    }
}
