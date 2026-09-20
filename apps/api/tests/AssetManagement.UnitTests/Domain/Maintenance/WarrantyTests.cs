using AssetManagement.Domain.Maintenance;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Maintenance;

public class WarrantyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_with_start_after_end_throws()
    {
        var act = () => Warranty.Create(
            Guid.NewGuid(), Guid.NewGuid(), WarrantyType.Manufacturer, "Dell", new DateOnly(2026, 1, 1),
            new DateOnly(2025, 1, 1), null, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_with_empty_provider_throws()
    {
        var act = () => Warranty.Create(
            Guid.NewGuid(), Guid.NewGuid(), WarrantyType.Manufacturer, "  ", new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1), null, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_succeeds_with_valid_dates()
    {
        var warranty = Warranty.Create(
            Guid.NewGuid(), Guid.NewGuid(), WarrantyType.Extended, "Dell ProSupport", new DateOnly(2026, 1, 1),
            new DateOnly(2028, 1, 1), "3 años, incluye partes y mano de obra.", Now, null);

        warranty.Provider.Should().Be("Dell ProSupport");
        warranty.Type.Should().Be(WarrantyType.Extended);
    }

    [Fact]
    public void UpdateDetails_with_start_after_end_throws()
    {
        var warranty = Warranty.Create(
            Guid.NewGuid(), Guid.NewGuid(), WarrantyType.Manufacturer, "Dell", new DateOnly(2026, 1, 1),
            new DateOnly(2027, 1, 1), null, Now, null);

        var act = () => warranty.UpdateDetails(
            WarrantyType.Manufacturer, "Dell", new DateOnly(2027, 1, 1), new DateOnly(2026, 1, 1), null, Now, null);

        act.Should().Throw<DomainException>();
    }
}
