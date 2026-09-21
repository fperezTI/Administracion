using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Inventory;

public class SignAssignmentCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private static async Task<(AssetManagement.Infrastructure.Persistence.AppDbContext Db, Asset Asset, Assignment Assignment, Guid RecipientUserId)>
        SeedPendingAssignmentAsync(Guid companyId)
    {
        var db = InMemoryAppDbContextFactory.Create(new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] });
        var asset = Asset.Create(
            companyId, Guid.NewGuid(), "ASSET-000001", "Dell", "Latitude 5450", null, null,
            PhysicalCondition.Excellent, Now, null);
        asset.ChangeStatus(AssetStatus.Reserved, Now, null);

        var movement = Movement.Create(
            companyId, asset.Id, MovementType.Assignment, "MOV-ASG-000001", startsCompleted: false,
            null, null, null, Guid.NewGuid(), null, Now, null);

        var recipientUserId = Guid.NewGuid();
        var assignment = Assignment.Create(companyId, asset.Id, recipientUserId, null, movement.Id, Now, null);

        db.Assets.Add(asset);
        db.Movements.Add(movement);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        return (db, asset, assignment, recipientUserId);
    }

    [Fact]
    public async Task Signing_by_the_named_recipient_accepts_and_moves_the_asset_to_Assigned()
    {
        var companyId = Guid.NewGuid();
        var (db, asset, assignment, recipientUserId) = await SeedPendingAssignmentAsync(companyId);
        var currentUser = new FakeCurrentUserContext { UserId = recipientUserId };

        var handler = new SignAssignmentCommandHandler(db, currentUser, new FakeClock(Now.AddHours(1)));
        await handler.Handle(new SignAssignmentCommand(assignment.Id, "Ada Lovelace"), CancellationToken.None);

        var reloadedAsset = await db.Assets.SingleAsync(a => a.Id == asset.Id);
        reloadedAsset.Status.Should().Be(AssetStatus.Assigned);

        var reloadedAssignment = await db.Assignments.SingleAsync(a => a.Id == assignment.Id);
        reloadedAssignment.Status.Should().Be(AssignmentStatus.Accepted);
        reloadedAssignment.SignatureRecordId.Should().NotBeNull();

        var signature = await db.SignatureRecords.SingleAsync(s => s.Id == reloadedAssignment.SignatureRecordId);
        signature.SignerDisplayName.Should().Be("Ada Lovelace");
        signature.ContextType.Should().Be("AssignmentAcceptance");
    }

    [Fact]
    public async Task Signing_one_member_of_a_group_accepts_the_whole_group()
    {
        var companyId = Guid.NewGuid();
        var db = InMemoryAppDbContextFactory.Create(new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] });
        var recipientUserId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var laptop = Asset.Create(
            companyId, Guid.NewGuid(), "ASSET-000001", "Dell", "Latitude 5450", null, null,
            PhysicalCondition.Excellent, Now, null);
        laptop.ChangeStatus(AssetStatus.Reserved, Now, null);
        var laptopMovement = Movement.Create(
            companyId, laptop.Id, MovementType.Assignment, "MOV-ASG-000001", startsCompleted: false,
            null, null, null, recipientUserId, null, Now, null);
        var laptopAssignment = Assignment.Create(companyId, laptop.Id, recipientUserId, null, laptopMovement.Id, Now, null, groupId);

        var charger = Asset.Create(
            companyId, Guid.NewGuid(), "ASSET-000002", "Dell", "Charger", null, null,
            PhysicalCondition.Excellent, Now, null);
        charger.ChangeStatus(AssetStatus.Reserved, Now, null);
        var chargerMovement = Movement.Create(
            companyId, charger.Id, MovementType.Assignment, "MOV-ASG-000002", startsCompleted: false,
            null, null, null, recipientUserId, null, Now, null);
        var chargerAssignment = Assignment.Create(companyId, charger.Id, recipientUserId, null, chargerMovement.Id, Now, null, groupId);

        db.Assets.AddRange(laptop, charger);
        db.Movements.AddRange(laptopMovement, chargerMovement);
        db.Assignments.AddRange(laptopAssignment, chargerAssignment);
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserContext { UserId = recipientUserId };
        var handler = new SignAssignmentCommandHandler(db, currentUser, new FakeClock(Now.AddHours(1)));

        await handler.Handle(new SignAssignmentCommand(laptopAssignment.Id, "Ada Lovelace"), CancellationToken.None);

        var reloadedCharger = await db.Assets.SingleAsync(a => a.Id == charger.Id);
        reloadedCharger.Status.Should().Be(AssetStatus.Assigned, "signing the primary accepts the whole group in one action");

        var reloadedChargerAssignment = await db.Assignments.SingleAsync(a => a.Id == chargerAssignment.Id);
        reloadedChargerAssignment.Status.Should().Be(AssignmentStatus.Accepted);
        reloadedChargerAssignment.SignatureRecordId.Should().NotBeNull();
    }

    [Fact]
    public async Task Signing_by_someone_other_than_the_recipient_is_forbidden()
    {
        var companyId = Guid.NewGuid();
        var (db, _, assignment, _) = await SeedPendingAssignmentAsync(companyId);
        var currentUser = new FakeCurrentUserContext { UserId = Guid.NewGuid() };

        var handler = new SignAssignmentCommandHandler(db, currentUser, new FakeClock(Now));
        var act = () => handler.Handle(new SignAssignmentCommand(assignment.Id, "Someone Else"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
