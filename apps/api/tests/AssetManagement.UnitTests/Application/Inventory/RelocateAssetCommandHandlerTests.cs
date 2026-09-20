using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Organization;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Inventory;

public class RelocateAssetCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Relocating_updates_the_org_unit_and_creates_a_completed_movement()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var asset = Asset.Create(
            companyId, Guid.NewGuid(), "ASSET-000001", "Dell", "Latitude", null, null,
            PhysicalCondition.Good, Now, null);
        var warehouseType = new OrgUnitType(Guid.NewGuid(), "WAREHOUSE", "Almacén");
        var newOrgUnit = OrgUnit.Create(companyId, warehouseType.Id, null, "Almacén CDMX", "ALM-CDMX", Now);
        db.Assets.Add(asset);
        db.OrgUnitTypes.Add(warehouseType);
        db.OrgUnits.Add(newOrgUnit);
        await db.SaveChangesAsync();

        var handler = new RelocateAssetCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));
        var result = await handler.Handle(new RelocateAssetCommand(asset.Id, newOrgUnit.Id, "Reubicación de prueba"), CancellationToken.None);

        var reloadedAsset = await db.Assets.SingleAsync(a => a.Id == asset.Id);
        reloadedAsset.CurrentOrgUnitId.Should().Be(newOrgUnit.Id);

        var movement = await db.Movements.SingleAsync(m => m.Id == result.MovementId);
        movement.ToOrgUnitId.Should().Be(newOrgUnit.Id);
    }

    [Fact]
    public async Task Relocating_to_the_same_org_unit_is_rejected()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var warehouseType = new OrgUnitType(Guid.NewGuid(), "WAREHOUSE", "Almacén");
        var orgUnit = OrgUnit.Create(companyId, warehouseType.Id, null, "Almacén CDMX", "ALM-CDMX", Now);
        var asset = Asset.Create(
            companyId, Guid.NewGuid(), "ASSET-000001", "Dell", "Latitude", null, null,
            PhysicalCondition.Good, Now, null);
        asset.MoveToOrgUnit(orgUnit.Id, Now, null);
        db.Assets.Add(asset);
        db.OrgUnitTypes.Add(warehouseType);
        db.OrgUnits.Add(orgUnit);
        await db.SaveChangesAsync();

        var handler = new RelocateAssetCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));
        var act = () => handler.Handle(new RelocateAssetCommand(asset.Id, orgUnit.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
