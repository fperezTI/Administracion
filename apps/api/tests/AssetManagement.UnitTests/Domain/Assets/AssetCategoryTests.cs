using AssetManagement.Domain.Assets;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Assets;

public class AssetCategoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AddCustomField_rejects_a_duplicate_code_within_the_category()
    {
        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        category.AddCustomField("RAM", "RAM", CustomFieldDataType.Text, isRequired: true, options: null);

        var act = () => category.AddCustomField("RAM otra vez", "RAM", CustomFieldDataType.Text, false, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddCustomField_assigns_increasing_sort_order()
    {
        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);

        var first = category.AddCustomField("CPU", "CPU", CustomFieldDataType.Text, false, null);
        var second = category.AddCustomField("RAM", "RAM", CustomFieldDataType.Text, false, null);

        second.SortOrder.Should().BeGreaterThan(first.SortOrder);
    }

    [Fact]
    public void AddCustomField_a_select_type_without_options_throws()
    {
        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);

        var act = () => category.AddCustomField("Color", "COLOR", CustomFieldDataType.Select, false, options: null);

        act.Should().Throw<DomainException>();
    }
}
