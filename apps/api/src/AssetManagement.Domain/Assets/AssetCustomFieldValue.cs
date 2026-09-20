namespace AssetManagement.Domain.Assets;

/// <summary>One value for one of the asset's category-defined custom fields. Stored as text; the
/// declared <see cref="CustomFieldDataType"/> on the definition governs how it's interpreted/validated at
/// the application layer, keeping the storage shape simple regardless of field type.</summary>
public sealed class AssetCustomFieldValue
{
    public Guid AssetId { get; private set; }
    public Guid CustomFieldDefinitionId { get; private set; }
    public string Value { get; private set; } = null!;

    private AssetCustomFieldValue()
    {
    }

    internal static AssetCustomFieldValue Create(Guid assetId, Guid customFieldDefinitionId, string value) =>
        new() { AssetId = assetId, CustomFieldDefinitionId = customFieldDefinitionId, Value = value };
}
