using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Assets;

/// <summary>
/// Configurable catalog of asset kinds (pedido §10) — seeded with the 9 initial categories (laptops,
/// monitores, docks, cargadores, celulares, impresoras, switches, access points, servidores) but never a
/// hardcoded enum, so operators can add categories later without a code change.
/// </summary>
public sealed class AssetCategory : AggregateRoot<Guid>
{
    private readonly List<CustomFieldDefinition> _customFields = [];

    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public IdentificationTechnology DefaultIdentificationTechnology { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<CustomFieldDefinition> CustomFields => _customFields.AsReadOnly();

    private AssetCategory()
    {
    }

    private AssetCategory(
        Guid id, string name, string code, IdentificationTechnology defaultTechnology, DateTimeOffset nowUtc)
        : base(id)
    {
        Name = name;
        Code = code;
        DefaultIdentificationTechnology = defaultTechnology;
        CreatedAtUtc = nowUtc;
        IsActive = true;
    }

    public static AssetCategory Create(
        string name, string code, IdentificationTechnology defaultTechnology, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("El nombre de la categoría es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("El código de la categoría es obligatorio.");
        }

        return new AssetCategory(Guid.NewGuid(), name.Trim(), code.Trim(), defaultTechnology, nowUtc);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public CustomFieldDefinition AddCustomField(
        string name, string code, CustomFieldDataType dataType, bool isRequired, string? options)
    {
        if (_customFields.Any(f => f.Code == code))
        {
            throw new DomainException($"Ya existe un campo personalizado con el código '{code}' en esta categoría.");
        }

        var sortOrder = _customFields.Count == 0 ? 0 : _customFields.Max(f => f.SortOrder) + 1;
        var field = CustomFieldDefinition.Create(Id, name, code, dataType, isRequired, sortOrder, options);
        _customFields.Add(field);
        return field;
    }
}
