using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Templates;

/// <summary>
/// A named, versioned piece of text (pedido: "resguardos, correos, notificaciones"). F4 only builds the
/// versioned catalog — no rendering/PDF generation, no placeholder substitution engine, and no
/// association with a specific signature or document yet: that depends on Blob Storage (Documents
/// context, F8, not built). Keeping this to a plain versioned text catalog avoids building a piece of F8
/// prematurely.
/// </summary>
public sealed class Template : AuditableAggregateRoot<Guid>
{
    private readonly List<TemplateVersion> _versions = [];

    public string Key { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<TemplateVersion> Versions => _versions.AsReadOnly();
    public TemplateVersion? LatestVersion => _versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

    private Template()
    {
    }

    private Template(Guid id, string key, string name, DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        Key = key;
        Name = name;
    }

    public static Template Create(
        string key, string name, string initialContent, DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException("La clave de la plantilla es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("El nombre de la plantilla es obligatorio.");
        }

        var template = new Template(Guid.NewGuid(), key.Trim(), name.Trim(), nowUtc, createdByUserId);
        template.AddVersion(initialContent, nowUtc, createdByUserId);
        return template;
    }

    public TemplateVersion AddVersion(string content, DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("El contenido de la plantilla es obligatorio.");
        }

        var nextVersionNumber = (LatestVersion?.VersionNumber ?? 0) + 1;
        var version = TemplateVersion.Create(Id, nextVersionNumber, content.Trim(), nowUtc, createdByUserId);
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
