using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Assets;

/// <summary>A category-specific technical/operational field (pedido §10/§11: "cada categoría podrá
/// definir campos personalizados y reglas de obligatoriedad") — e.g. CPU/RAM for laptops, IMEI for
/// phones.</summary>
public sealed class CustomFieldDefinition : Entity<Guid>
{
    public Guid AssetCategoryId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public CustomFieldDataType DataType { get; private set; }
    public bool IsRequired { get; private set; }
    public int SortOrder { get; private set; }

    /// <summary>Comma-separated allowed values — only meaningful when DataType is Select.</summary>
    public string? Options { get; private set; }

    private CustomFieldDefinition()
    {
    }

    private CustomFieldDefinition(
        Guid id, Guid assetCategoryId, string name, string code, CustomFieldDataType dataType,
        bool isRequired, int sortOrder, string? options)
        : base(id)
    {
        AssetCategoryId = assetCategoryId;
        Name = name;
        Code = code;
        DataType = dataType;
        IsRequired = isRequired;
        SortOrder = sortOrder;
        Options = options;
    }

    internal static CustomFieldDefinition Create(
        Guid assetCategoryId, string name, string code, CustomFieldDataType dataType, bool isRequired,
        int sortOrder, string? options)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("El nombre del campo personalizado es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("El código del campo personalizado es obligatorio.");
        }

        if (dataType == CustomFieldDataType.Select && string.IsNullOrWhiteSpace(options))
        {
            throw new DomainException("Un campo de selección debe definir sus opciones.");
        }

        return new CustomFieldDefinition(
            Guid.NewGuid(), assetCategoryId, name.Trim(), code.Trim(), dataType, isRequired, sortOrder, options);
    }
}
