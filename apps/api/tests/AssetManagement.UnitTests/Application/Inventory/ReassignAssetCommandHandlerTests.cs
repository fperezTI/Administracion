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

public class ReassignAssetCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private static User CreateUserWithCompanyAccess(string displayName, string email, Guid companyId)
    {
        var user = User.Provision(Guid.NewGuid(), displayName, email, Now);
        user.GrantCompanyAccess(companyId, Now);
        return user;
    }

    private static (Asset Asset, Movement Movement, Assignment Assignment) CreateAssignedAsset(Guid companyId, Guid currentUserId)
    {
        var asset = Asset.Create(
            companyId, Guid.NewGuid(), "ASSET-000001", "Dell", "Latitude 5450", "SN-1", null,
            PhysicalCondition.Excellent, Now, null);
        var movement = Movement.Create(
            companyId, asset.Id, MovementType.Assignment, "MOV-ASG-000001", startsCompleted: false,
            fromOrgUnitId: null, toOrgUnitId: null, fromUserId: null, toUserId: currentUserId, notes: null,
            Now, null);
        var assignment = Assignment.Create(companyId, asset.Id, currentUserId, null, movement.Id, Now, null);
        assignment.Accept(Guid.NewGuid(), Now, currentUserId);
        movement.Complete(Now, currentUserId);
        asset.ChangeStatus(AssetStatus.Assigned, Now, null);
        return (asset, movement, assignment);
    }

    [Fact]
    public async Task Returns_the_current_assignment_and_creates_a_new_pending_one_atomically()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var currentRecipient = CreateUserWithCompanyAccess("Ada Lovelace", "ada@example.com", companyId);
        var (asset, movement, assignment) = CreateAssignedAsset(companyId, currentRecipient.Id);
        var newRecipient = CreateUserWithCompanyAccess("Grace Hopper", "grace@example.com", companyId);
        db.Users.Add(currentRecipient);
        db.Users.Add(newRecipient);
        db.Assets.Add(asset);
        db.Movements.Add(movement);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        var handler = new ReassignAssetCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var result = await handler.Handle(
            new ReassignAssetCommand(asset.Id, newRecipient.Id, null, "Someone From IT", null), CancellationToken.None);

        var reloadedAsset = await db.Assets.SingleAsync(a => a.Id == asset.Id);
        reloadedAsset.Status.Should().Be(AssetStatus.Reserved);

        var oldAssignment = await db.Assignments.SingleAsync(a => a.Id == assignment.Id);
        oldAssignment.Status.Should().Be(AssignmentStatus.Returned);

        var newAssignment = await db.Assignments.SingleAsync(a => a.Id == result.AssignmentId);
        newAssignment.Status.Should().Be(AssignmentStatus.PendingSignature);
        newAssignment.AssignedToUserId.Should().Be(newRecipient.Id);

        var newMovement = await db.Movements.SingleAsync(m => m.Id == result.MovementId);
        newMovement.Type.Should().Be(MovementType.Assignment);
        newMovement.Status.Should().Be(MovementStatus.Pending);
    }

    private static (Asset Asset, Movement Movement, Assignment Assignment) CreateGroupMember(
        Guid companyId, Guid currentUserId, Guid groupId, string internalFolio)
    {
        var asset = Asset.Create(
            companyId, Guid.NewGuid(), internalFolio, "Dell", "Charger", $"SN-{internalFolio}", null,
            PhysicalCondition.Excellent, Now, null);
        var movement = Movement.Create(
            companyId, asset.Id, MovementType.Assignment, $"MOV-{internalFolio}", startsCompleted: false,
            fromOrgUnitId: null, toOrgUnitId: null, fromUserId: null, toUserId: currentUserId, notes: null,
            Now, null);
        var assignment = Assignment.Create(companyId, asset.Id, currentUserId, null, movement.Id, Now, null, groupId);
        assignment.Accept(Guid.NewGuid(), Now, currentUserId);
        movement.Complete(Now, currentUserId);
        asset.ChangeStatus(AssetStatus.Assigned, Now, null);
        return (asset, movement, assignment);
    }

    [Fact]
    public async Task Reassigning_the_primary_returns_and_recreates_the_whole_group()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var currentRecipient = CreateUserWithCompanyAccess("Ada Lovelace", "ada@example.com", companyId);
        var newRecipient = CreateUserWithCompanyAccess("Grace Hopper", "grace@example.com", companyId);
        var groupId = Guid.NewGuid();
        var (laptop, laptopMovement, laptopAssignment) = CreateGroupMember(companyId, currentRecipient.Id, groupId, "ASSET-000001");
        var (charger, chargerMovement, chargerAssignment) = CreateGroupMember(companyId, currentRecipient.Id, groupId, "ASSET-000002");
        charger.LinkAsAccessoryOf(laptop.Id, Now, null);
        db.Users.AddRange(currentRecipient, newRecipient);
        db.Assets.AddRange(laptop, charger);
        db.Movements.AddRange(laptopMovement, chargerMovement);
        db.Assignments.AddRange(laptopAssignment, chargerAssignment);
        await db.SaveChangesAsync();

        var handler = new ReassignAssetCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var result = await handler.Handle(
            new ReassignAssetCommand(laptop.Id, newRecipient.Id, null, "Someone From IT", null, [charger.Id]),
            CancellationToken.None);

        var oldLaptopAssignment = await db.Assignments.SingleAsync(a => a.Id == laptopAssignment.Id);
        var oldChargerAssignment = await db.Assignments.SingleAsync(a => a.Id == chargerAssignment.Id);
        oldLaptopAssignment.Status.Should().Be(AssignmentStatus.Returned);
        oldChargerAssignment.Status.Should().Be(AssignmentStatus.Returned, "the whole old group moves together, not just the primary");

        var newLaptopAssignment = await db.Assignments.SingleAsync(a => a.Id == result.AssignmentId);
        var newChargerAssignment = await db.Assignments.SingleAsync(a => a.AssetId == charger.Id && a.Status == AssignmentStatus.PendingSignature);
        newLaptopAssignment.AssignmentGroupId.Should().NotBeNull().And.Be(newChargerAssignment.AssignmentGroupId);
        newLaptopAssignment.AssignmentGroupId.Should().NotBe(groupId, "this is a fresh group, not the old one");
        newChargerAssignment.AssignedToUserId.Should().Be(newRecipient.Id);
    }

    [Fact]
    public async Task Rejects_when_the_asset_has_no_active_assignment()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var asset = Asset.Create(
            companyId, Guid.NewGuid(), "ASSET-000002", "HP", "EliteBook", "SN-2", null,
            PhysicalCondition.Excellent, Now, null);
        var newRecipient = CreateUserWithCompanyAccess("Grace Hopper", "grace@example.com", companyId);
        db.Assets.Add(asset);
        db.Users.Add(newRecipient);
        await db.SaveChangesAsync();

        var handler = new ReassignAssetCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var act = () => handler.Handle(
            new ReassignAssetCommand(asset.Id, newRecipient.Id, null, "Someone From IT", null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Rejects_a_new_recipient_without_access_to_the_assets_company()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var currentRecipient = CreateUserWithCompanyAccess("Ada Lovelace", "ada@example.com", companyId);
        var (asset, movement, assignment) = CreateAssignedAsset(companyId, currentRecipient.Id);
        var newRecipientWithoutAccess = User.Provision(Guid.NewGuid(), "Grace Hopper", "grace@example.com", Now);
        db.Users.Add(currentRecipient);
        db.Users.Add(newRecipientWithoutAccess);
        db.Assets.Add(asset);
        db.Movements.Add(movement);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        var handler = new ReassignAssetCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var act = () => handler.Handle(
            new ReassignAssetCommand(asset.Id, newRecipientWithoutAccess.Id, null, "Someone From IT", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
