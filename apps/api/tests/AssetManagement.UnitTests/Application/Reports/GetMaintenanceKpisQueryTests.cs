using AssetManagement.Application.Reports;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.Reports;

public class GetMaintenanceKpisQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Computes_mttr_and_mtbf_from_two_closed_orders_on_the_same_asset()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);

        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        db.AssetCategories.Add(category);
        var asset = Asset.Create(companyId, category.Id, "ASSET-000001", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null);
        db.Assets.Add(asset);

        var order1 = MaintenanceOrder.Open(companyId, asset.Id, "MAINT-000001", MaintenanceOrderType.Corrective, "Falla 1", null, null, null, Now, null);
        order1.Close(AssetStatus.InWarehouse, "Reparado.", null, Now.AddHours(2), null); // 2h duration
        db.MaintenanceOrders.Add(order1);

        var secondOpen = Now.AddDays(10);
        var order2 = MaintenanceOrder.Open(companyId, asset.Id, "MAINT-000002", MaintenanceOrderType.Corrective, "Falla 2", null, null, null, secondOpen, null);
        order2.Close(AssetStatus.InWarehouse, "Reparado.", null, secondOpen.AddHours(4), null); // 4h duration
        db.MaintenanceOrders.Add(order2);

        await db.SaveChangesAsync();

        var result = await new GetMaintenanceKpisQueryHandler(db, companyContext).Handle(new GetMaintenanceKpisQuery(companyId), CancellationToken.None);

        result.ClosedOrdersCount.Should().Be(2);
        result.MttrHours.Should().Be(3); // average of 2h and 4h
        result.MtbfDays.Should().Be(10); // single gap between the two OpenedAtUtc
        result.ByCategory.Should().ContainSingle(c => c.AssetCategoryId == category.Id && c.ClosedOrdersCount == 2);
    }

    [Fact]
    public async Task Mtbf_is_null_when_no_asset_has_more_than_one_order()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);

        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        db.AssetCategories.Add(category);
        var asset = Asset.Create(companyId, category.Id, "ASSET-000001", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null);
        db.Assets.Add(asset);

        var order = MaintenanceOrder.Open(companyId, asset.Id, "MAINT-000001", MaintenanceOrderType.Corrective, "Falla", null, null, null, Now, null);
        order.Close(AssetStatus.InWarehouse, "Reparado.", null, Now.AddHours(1), null);
        db.MaintenanceOrders.Add(order);
        await db.SaveChangesAsync();

        var result = await new GetMaintenanceKpisQueryHandler(db, companyContext).Handle(new GetMaintenanceKpisQuery(companyId), CancellationToken.None);

        result.MtbfDays.Should().BeNull();
        result.MttrHours.Should().Be(1);
    }

    [Fact]
    public async Task Open_orders_are_excluded_from_mttr()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);

        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        db.AssetCategories.Add(category);
        var asset = Asset.Create(companyId, category.Id, "ASSET-000001", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null);
        db.Assets.Add(asset);
        db.MaintenanceOrders.Add(MaintenanceOrder.Open(companyId, asset.Id, "MAINT-000001", MaintenanceOrderType.Corrective, "Falla", null, null, null, Now, null));
        await db.SaveChangesAsync();

        var result = await new GetMaintenanceKpisQueryHandler(db, companyContext).Handle(new GetMaintenanceKpisQuery(companyId), CancellationToken.None);

        result.ClosedOrdersCount.Should().Be(0);
        result.MttrHours.Should().BeNull();
    }
}
