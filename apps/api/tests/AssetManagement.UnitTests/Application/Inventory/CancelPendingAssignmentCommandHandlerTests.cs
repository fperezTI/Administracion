using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Inventory;

public class CancelPendingAssignmentCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private static (Asset Asset, Movement Movement, Assignment Assignment) CreatePendingAssignment(
        Guid companyId, Guid recipientUserId, Guid? groupId, string internalFolio)
    {
        var asset = Asset.Create(
            companyId, Guid.NewGuid(), internalFolio, "Dell", "Latitude 5450", null, null,
            PhysicalCondition.Excellent, Now, null);
        asset.ChangeStatus(AssetStatus.Reserved, Now, null);
        var movement = Movement.Create(
            companyId, asset.Id, MovementType.Assignment, $"MOV-{internalFolio}", startsCompleted: false,
            fromOrgUnitId: null, toOrgUnitId: null, fromUserId: null, toUserId: recipientUserId, notes: null,
            Now, null);
        var assignment = Assignment.Create(companyId, asset.Id, recipientUserId, null, movement.Id, Now, null, groupId);
        return (asset, movement, assignment);
    }

    [Fact]
    public async Task Cancels_a_solo_pending_assignment()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var (asset, movement, assignment) = CreatePendingAssignment(companyId, Guid.NewGuid(), null, "ASSET-000001");
        db.Assets.Add(asset);
        db.Movements.Add(movement);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        var handler = new CancelPendingAssignmentCommandHandler(db, companyContext, new FakeCurrentUserContext(), new FakeClock(Now));
        await handler.Handle(new CancelPendingAssignmentCommand(assignment.Id), CancellationToken.None);

        var reloadedAsset = await db.Assets.SingleAsync(a => a.Id == asset.Id);
        reloadedAsset.Status.Should().Be(AssetStatus.InWarehouse);

        var reloadedAssignment = await db.Assignments.SingleAsync(a => a.Id == assignment.Id);
        reloadedAssignment.Status.Should().Be(AssignmentStatus.Cancelled);
    }

    [Fact]
    public async Task Cancelling_one_member_of_a_group_cascades_to_the_whole_group()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var recipientUserId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var (laptop, laptopMovement, laptopAssignment) = CreatePendingAssignment(companyId, recipientUserId, groupId, "ASSET-000001");
        var (charger, chargerMovement, chargerAssignment) = CreatePendingAssignment(companyId, recipientUserId, groupId, "ASSET-000002");
        db.Assets.AddRange(laptop, charger);
        db.Movements.AddRange(laptopMovement, chargerMovement);
        db.Assignments.AddRange(laptopAssignment, chargerAssignment);
        await db.SaveChangesAsync();

        var handler = new CancelPendingAssignmentCommandHandler(db, companyContext, new FakeCurrentUserContext(), new FakeClock(Now));
        await handler.Handle(new CancelPendingAssignmentCommand(laptopAssignment.Id), CancellationToken.None);

        var reloadedCharger = await db.Assets.SingleAsync(a => a.Id == charger.Id);
        reloadedCharger.Status.Should().Be(AssetStatus.InWarehouse, "cancelling the primary cancels the whole group");

        var reloadedChargerAssignment = await db.Assignments.SingleAsync(a => a.Id == chargerAssignment.Id);
        reloadedChargerAssignment.Status.Should().Be(AssignmentStatus.Cancelled);
    }
}
