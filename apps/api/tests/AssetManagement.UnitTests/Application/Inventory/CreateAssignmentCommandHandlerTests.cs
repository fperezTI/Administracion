using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Inventory;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Inventory;

public class CreateAssignmentCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private static Asset CreateWarehouseAsset(Guid companyId, string internalFolio = "ASSET-000001") => Asset.Create(
        companyId, Guid.NewGuid(), internalFolio, "Dell", "Latitude 5450", $"SN-{internalFolio}", null,
        PhysicalCondition.Excellent, Now, null);

    private static User CreateUserWithCompanyAccess(Guid companyId)
    {
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        user.GrantCompanyAccess(companyId, Now);
        return user;
    }

    [Fact]
    public async Task Creates_a_pending_assignment_and_moves_the_asset_to_Reserved()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var asset = CreateWarehouseAsset(companyId);
        var recipient = CreateUserWithCompanyAccess(companyId);
        db.Assets.Add(asset);
        db.Users.Add(recipient);
        await db.SaveChangesAsync();

        var handler = new CreateAssignmentCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var result = await handler.Handle(new CreateAssignmentCommand(asset.Id, recipient.Id, null, null), CancellationToken.None);

        var reloadedAsset = await db.Assets.SingleAsync(a => a.Id == asset.Id);
        reloadedAsset.Status.Should().Be(AssetStatus.Reserved);

        var assignment = await db.Assignments.SingleAsync(a => a.Id == result.AssignmentId);
        assignment.Status.Should().Be(AssignmentStatus.PendingSignature);

        var movement = await db.Movements.SingleAsync(m => m.Id == result.MovementId);
        movement.Type.Should().Be(MovementType.Assignment);
        movement.Status.Should().Be(MovementStatus.Pending);
    }

    [Fact]
    public async Task Rejects_a_recipient_without_access_to_the_assets_company()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var asset = CreateWarehouseAsset(companyId);
        var recipient = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        db.Assets.Add(asset);
        db.Users.Add(recipient);
        await db.SaveChangesAsync();

        var handler = new CreateAssignmentCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var act = () => handler.Handle(new CreateAssignmentCommand(asset.Id, recipient.Id, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Includes_accessories_in_the_same_group_as_the_primary_asset()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var laptop = CreateWarehouseAsset(companyId, "ASSET-000001");
        var charger = CreateWarehouseAsset(companyId, "ASSET-000002");
        charger.LinkAsAccessoryOf(laptop.Id, Now, null);
        var recipient = CreateUserWithCompanyAccess(companyId);
        db.Assets.Add(laptop);
        db.Assets.Add(charger);
        db.Users.Add(recipient);
        await db.SaveChangesAsync();

        var handler = new CreateAssignmentCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var result = await handler.Handle(
            new CreateAssignmentCommand(laptop.Id, recipient.Id, null, null, [charger.Id]), CancellationToken.None);

        var laptopAssignment = await db.Assignments.SingleAsync(a => a.Id == result.AssignmentId);
        var chargerAssignment = await db.Assignments.SingleAsync(a => a.AssetId == charger.Id);
        laptopAssignment.AssignmentGroupId.Should().NotBeNull();
        chargerAssignment.AssignmentGroupId.Should().Be(laptopAssignment.AssignmentGroupId);

        var reloadedCharger = await db.Assets.SingleAsync(a => a.Id == charger.Id);
        reloadedCharger.Status.Should().Be(AssetStatus.Reserved);
    }

    [Fact]
    public async Task Rejects_an_accessory_that_is_not_linked_to_the_primary_asset()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var laptop = CreateWarehouseAsset(companyId, "ASSET-000001");
        var unrelatedAsset = CreateWarehouseAsset(companyId, "ASSET-000002");
        var recipient = CreateUserWithCompanyAccess(companyId);
        db.Assets.Add(laptop);
        db.Assets.Add(unrelatedAsset);
        db.Users.Add(recipient);
        await db.SaveChangesAsync();

        var handler = new CreateAssignmentCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var act = () => handler.Handle(
            new CreateAssignmentCommand(laptop.Id, recipient.Id, null, null, [unrelatedAsset.Id]), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Rejects_an_accessory_that_is_not_available_in_the_warehouse()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var laptop = CreateWarehouseAsset(companyId, "ASSET-000001");
        var charger = CreateWarehouseAsset(companyId, "ASSET-000002");
        charger.LinkAsAccessoryOf(laptop.Id, Now, null);
        charger.ChangeStatus(AssetStatus.InMaintenance, Now, null);
        var recipient = CreateUserWithCompanyAccess(companyId);
        db.Assets.Add(laptop);
        db.Assets.Add(charger);
        db.Users.Add(recipient);
        await db.SaveChangesAsync();

        var handler = new CreateAssignmentCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var act = () => handler.Handle(
            new CreateAssignmentCommand(laptop.Id, recipient.Id, null, null, [charger.Id]), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Rejects_when_the_caller_does_not_have_access_to_the_assets_company()
    {
        // The global query filter on Asset (defense in depth, same as every other company-scoped
        // query) already hides the row before the handler's own explicit check would run — so an
        // inaccessible asset surfaces as "not found", not "forbidden". That's deliberate: it doesn't
        // leak whether an asset id exists outside the caller's accessible companies.
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var asset = CreateWarehouseAsset(companyId);
        db.Assets.Add(asset);
        await db.SaveChangesAsync();

        var handler = new CreateAssignmentCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var act = () => handler.Handle(new CreateAssignmentCommand(asset.Id, Guid.NewGuid(), null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
