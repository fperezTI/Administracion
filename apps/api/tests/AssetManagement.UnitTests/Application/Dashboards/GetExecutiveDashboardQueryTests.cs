using AssetManagement.Application.Dashboards;
using AssetManagement.Domain.Approvals;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using AssetManagement.Domain.SparePartsAndConsumables;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.Dashboards;

public class GetExecutiveDashboardQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Aggregates_every_kpi_for_a_single_company()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);

        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        db.AssetCategories.Add(category);

        var asset1 = Asset.Create(companyId, category.Id, "ASSET-000001", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null);
        var asset2 = Asset.Create(companyId, category.Id, "ASSET-000002", "HP", "EliteBook", null, null, PhysicalCondition.Good, Now, null);
        db.Assets.AddRange(asset1, asset2);

        var flow = ApprovalFlowDefinition.Create("asset.decommission", null, [Guid.NewGuid()], 1, ApprovalMode.Parallel, false, Now, null);
        db.ApprovalFlowDefinitions.Add(flow);
        db.ApprovalInstances.Add(ApprovalInstance.Create(flow, companyId, "AssetDecommission", asset1.Id, Guid.NewGuid(), null, Now, null));

        db.MaintenanceOrders.Add(MaintenanceOrder.Open(
            companyId, asset1.Id, "MAINT-000001", MaintenanceOrderType.Corrective, "Falla", null, null, null, Now, null));

        var today = DateOnly.FromDateTime(Now.UtcDateTime);
        db.Warranties.Add(Warranty.Create(companyId, asset1.Id, WarrantyType.Manufacturer, "Dell", today.AddDays(-100), today.AddDays(15), null, Now, null));

        var lowStock = Consumable.Create(companyId, "Toner HP 26A", "TNR-26A", "Pieza", 10, Now, null);
        lowStock.ApplyStockMovement(ConsumableStockDirection.In, 5, Now, null);
        db.Consumables.Add(lowStock);

        await db.SaveChangesAsync();

        var handler = new GetExecutiveDashboardQueryHandler(db, companyContext, new FakeClock(Now));
        var result = await handler.Handle(new GetExecutiveDashboardQuery(companyId), CancellationToken.None);

        result.TotalAssets.Should().Be(2);
        result.AvailableAssets.Should().Be(2);
        result.AssignedAssets.Should().Be(0);
        result.PendingApprovals.Should().Be(1);
        result.OpenMaintenanceOrders.Should().Be(1);
        result.ExpiringWarranties.Should().Be(1);
        result.LowStockConsumables.Should().Be(1);
    }

    [Fact]
    public async Task Consolidated_mode_aggregates_assets_across_every_accessible_company()
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

        var handler = new GetExecutiveDashboardQueryHandler(db, companyContext, new FakeClock(Now));
        var result = await handler.Handle(new GetExecutiveDashboardQuery(CompanyId: null), CancellationToken.None);

        result.TotalAssets.Should().Be(2);
    }
}
