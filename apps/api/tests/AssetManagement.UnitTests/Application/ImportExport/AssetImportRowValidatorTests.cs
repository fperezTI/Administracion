using AssetManagement.Application.ImportExport;
using AssetManagement.Domain.Assets;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.ImportExport;

public class AssetImportRowValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    private static AssetCategory CreateCategory(bool withRequiredCustomField = false)
    {
        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        if (withRequiredCustomField)
        {
            category.AddCustomField("RAM", "RAM", CustomFieldDataType.Text, isRequired: true, options: null);
        }

        return category;
    }

    private static AssetImportRowValidator CreateValidator(AssetCategory category, IEnumerable<string>? existingSerials = null) =>
        new(
            new Dictionary<string, AssetCategory>(StringComparer.OrdinalIgnoreCase) { [category.Code] = category },
            new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(existingSerials ?? [], StringComparer.OrdinalIgnoreCase));

    private static Dictionary<string, string> ValidRow(AssetCategory category) => new(StringComparer.OrdinalIgnoreCase)
    {
        [AssetImportColumns.AssetCategoryCode] = category.Code,
        [AssetImportColumns.Brand] = "Dell",
        [AssetImportColumns.Model] = "Latitude 5420",
        [AssetImportColumns.SerialNumber] = "SN-001",
        [AssetImportColumns.PhysicalCondition] = "Excellent",
    };

    [Fact]
    public void Validate_a_well_formed_row_succeeds()
    {
        var category = CreateCategory();
        var validator = CreateValidator(category);

        var (row, errors) = validator.Validate(ValidRow(category));

        errors.Should().BeEmpty();
        row.Should().NotBeNull();
        row!.AssetCategoryId.Should().Be(category.Id);
        row.Brand.Should().Be("Dell");
    }

    [Fact]
    public void Validate_unknown_category_code_fails()
    {
        var category = CreateCategory();
        var validator = CreateValidator(category);
        var raw = ValidRow(category);
        raw[AssetImportColumns.AssetCategoryCode] = "DOES-NOT-EXIST";

        var (row, errors) = validator.Validate(raw);

        row.Should().BeNull();
        errors.Should().Contain(e => e.Contains("DOES-NOT-EXIST"));
    }

    [Fact]
    public void Validate_inactive_category_fails()
    {
        var category = CreateCategory();
        category.Deactivate();
        var validator = CreateValidator(category);

        var (row, errors) = validator.Validate(ValidRow(category));

        row.Should().BeNull();
        errors.Should().Contain(e => e.Contains("desactivada"));
    }

    [Fact]
    public void Validate_missing_required_custom_field_fails()
    {
        var category = CreateCategory(withRequiredCustomField: true);
        var validator = CreateValidator(category);

        var (row, errors) = validator.Validate(ValidRow(category));

        row.Should().BeNull();
        errors.Should().Contain(e => e.Contains("RAM"));
    }

    [Fact]
    public void Validate_present_required_custom_field_succeeds()
    {
        var category = CreateCategory(withRequiredCustomField: true);
        var validator = CreateValidator(category);
        var raw = ValidRow(category);
        raw[$"{AssetImportColumns.CustomFieldPrefix}RAM"] = "16GB";

        var (row, errors) = validator.Validate(raw);

        errors.Should().BeEmpty();
        row.Should().NotBeNull();
        row!.CustomFieldValues.Values.Should().Contain("16GB");
    }

    [Fact]
    public void Validate_serial_number_duplicated_against_existing_assets_fails()
    {
        var category = CreateCategory();
        var validator = CreateValidator(category, existingSerials: ["SN-001"]);

        var (row, errors) = validator.Validate(ValidRow(category));

        row.Should().BeNull();
        errors.Should().Contain(e => e.Contains("SN-001"));
    }

    [Fact]
    public void Validate_serial_number_duplicated_within_the_same_batch_fails_on_second_occurrence()
    {
        var category = CreateCategory();
        var validator = CreateValidator(category);

        var (firstRow, firstErrors) = validator.Validate(ValidRow(category));
        var (secondRow, secondErrors) = validator.Validate(ValidRow(category));

        firstRow.Should().NotBeNull();
        firstErrors.Should().BeEmpty();
        secondRow.Should().BeNull();
        secondErrors.Should().Contain(e => e.Contains("duplicado"));
    }

    [Fact]
    public void Validate_missing_physical_condition_fails()
    {
        var category = CreateCategory();
        var validator = CreateValidator(category);
        var raw = ValidRow(category);
        raw.Remove(AssetImportColumns.PhysicalCondition);

        var (row, errors) = validator.Validate(raw);

        row.Should().BeNull();
        errors.Should().Contain(e => e.Contains(AssetImportColumns.PhysicalCondition));
    }
}
