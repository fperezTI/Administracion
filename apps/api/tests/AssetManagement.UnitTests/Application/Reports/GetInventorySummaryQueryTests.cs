using AssetManagement.Application.Reports;
using AssetManagement.Domain.Assets;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.Reports;

public class GetInventorySummaryQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Counts_assets_by_status_and_by_category_for_a_single_company()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);

        var laptops = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        var monitors = AssetCategory.Create("Monitores", "MONITOR", IdentificationTechnology.Qr, Now);
        db.AssetCategories.AddRange(laptops, monitors);

        db.Assets.Add(Asset.Create(companyId, laptops.Id, "ASSET-000001", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null));
        db.Assets.Add(Asset.Create(companyId, laptops.Id, "ASSET-000002", "HP", "EliteBook", null, null, PhysicalCondition.Good, Now, null));
        db.Assets.Add(Asset.Create(companyId, monitors.Id, "ASSET-000003", "Dell", "P2419", null, null, PhysicalCondition.Good, Now, null));
        await db.SaveChangesAsync();

        var handler = new GetInventorySummaryQueryHandler(db, companyContext);
        var result = await handler.Handle(new GetInventorySummaryQuery(companyId), CancellationToken.None);

        result.TotalAssets.Should().Be(3);
        result.ByStatus.Should().ContainSingle(s => s.Status == AssetStatus.InWarehouse && s.Count == 3);
        result.ByCategory.Should().Contain(c => c.AssetCategoryId == laptops.Id && c.Count == 2);
        result.ByCategory.Should().Contain(c => c.AssetCategoryId == monitors.Id && c.Count == 1);
    }

    [Fact]
    public async Task Consolidated_mode_aggregates_across_every_accessible_company()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyA, companyB] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);

        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        db.AssetCategories.Add(category);
        db.Assets.Add(Asset.Create(companyA, category.Id, "ASSET-A1", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null));
        db.Assets.Add(Asset.Create(companyB, category.Id, "ASSET-B1", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null));
        await db.SaveChangesAsync();

        var handler = new GetInventorySummaryQueryHandler(db, companyContext);
        var result = await handler.Handle(new GetInventorySummaryQuery(CompanyId: null), CancellationToken.None);

        result.TotalAssets.Should().Be(2);
    }
}
