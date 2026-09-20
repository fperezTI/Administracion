using AssetManagement.Domain.Assets;

namespace AssetManagement.Application.ImportExport;

/// <summary>Fixed CSV column names for the V1 asset-import template (ADR 0011). Custom-field columns are
/// named <c>CustomField:{Code}</c>, one per <see cref="Domain.Assets.CustomFieldDefinition.Code"/> of the
/// row's own category — resolved dynamically, not listed here.</summary>
public static class AssetImportColumns
{
    public const string AssetCategoryCode = "AssetCategoryCode";
    public const string Brand = "Brand";
    public const string Model = "Model";
    public const string SerialNumber = "SerialNumber";
    public const string Description = "Description";
    public const string PhysicalCondition = "PhysicalCondition";
    public const string OrgUnitCode = "OrgUnitCode";
    public const string IdentificationTechnology = "IdentificationTechnology";

    public const string CustomFieldPrefix = "CustomField:";

    public static readonly IReadOnlyList<string> FixedColumns =
    [
        AssetCategoryCode, Brand, Model, SerialNumber, Description, PhysicalCondition, OrgUnitCode, IdentificationTechnology,
    ];
}

/// <summary>A single row resolved out of a raw CSV import, ready to create an <see cref="Asset"/> — only
/// produced by <see cref="AssetImportRowValidator"/> when every field is valid.</summary>
public sealed record ResolvedAssetImportRow(
    Guid AssetCategoryId,
    string Brand,
    string Model,
    string? SerialNumber,
    string? Description,
    PhysicalCondition PhysicalCondition,
    Guid? CurrentOrgUnitId,
    IdentificationTechnology? IdentificationTechnologyOverride,
    IReadOnlyDictionary<Guid, string> CustomFieldValues);

public sealed record ImportRowResult(int RowNumber, bool Success, IReadOnlyList<string> Errors, string? CreatedAssetFolio)
{
    public static ImportRowResult Ok(int rowNumber, string? createdAssetFolio = null) => new(rowNumber, true, [], createdAssetFolio);

    public static ImportRowResult Fail(int rowNumber, IReadOnlyList<string> errors) => new(rowNumber, false, errors, null);
}
