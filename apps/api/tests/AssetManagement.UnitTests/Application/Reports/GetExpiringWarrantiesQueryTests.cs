using AssetManagement.Application.Reports;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.Reports;

public class GetExpiringWarrantiesQueryTests
{
    private static readonly DateTimeOffset Today = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Includes_warranties_within_the_window_and_already_expired_excludes_ones_far_in_the_future()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);

        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Today);
        db.AssetCategories.Add(category);
        var asset = Asset.Create(companyId, category.Id, "ASSET-000001", "Dell", "Latitude", null, null, PhysicalCondition.Good, Today, null);
        db.Assets.Add(asset);

        var today = DateOnly.FromDateTime(Today.UtcDateTime);
        db.Warranties.Add(Warranty.Create(companyId, asset.Id, WarrantyType.Manufacturer, "Dell", today.AddDays(-100), today.AddDays(-5), null, Today, null)); // already expired
        db.Warranties.Add(Warranty.Create(companyId, asset.Id, WarrantyType.Manufacturer, "Dell", today.AddDays(-100), today.AddDays(15), null, Today, null)); // expiring within window
        db.Warranties.Add(Warranty.Create(companyId, asset.Id, WarrantyType.Manufacturer, "Dell", today.AddDays(-100), today.AddDays(90), null, Today, null)); // far in the future
        await db.SaveChangesAsync();

        var handler = new GetExpiringWarrantiesQueryHandler(db, companyContext, new FakeClock(Today));
        var result = await handler.Handle(new GetExpiringWarrantiesQuery(companyId, WithinDays: 30), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(r => r.DaysRemaining == -5);
        result.Should().Contain(r => r.DaysRemaining == 15);
        result.Should().NotContain(r => r.DaysRemaining == 90);
        result.Select(r => r.EndDate).Should().BeInAscendingOrder();
    }
}
