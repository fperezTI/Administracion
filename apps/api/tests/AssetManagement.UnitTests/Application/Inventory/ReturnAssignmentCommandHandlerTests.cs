using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Inventory;

public class ReturnAssignmentCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private static (Asset Asset, Movement Movement, Assignment Assignment) CreateAcceptedAssignment(
        Guid companyId, Guid recipientUserId, Guid? groupId, string internalFolio)
    {
        var asset = Asset.Create(
            companyId, Guid.NewGuid(), internalFolio, "Dell", "Latitude 5450", $"SN-{internalFolio}", null,
            PhysicalCondition.Excellent, Now, null);
        var movement = Movement.Create(
            companyId, asset.Id, MovementType.Assignment, $"MOV-{internalFolio}", startsCompleted: false,
            fromOrgUnitId: null, toOrgUnitId: null, fromUserId: null, toUserId: recipientUserId, notes: null,
            Now, null);
        var assignment = Assignment.Create(companyId, asset.Id, recipientUserId, null, movement.Id, Now, null, groupId);
        assignment.Accept(Guid.NewGuid(), Now, recipientUserId);
        movement.Complete(Now, recipientUserId);
        asset.ChangeStatus(AssetStatus.Assigned, Now, null);
        return (asset, movement, assignment);
    }

    [Fact]
    public async Task Returns_a_solo_assignment()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var recipientUserId = Guid.NewGuid();
        var (asset, movement, assignment) = CreateAcceptedAssignment(companyId, recipientUserId, null, "ASSET-000001");
        db.Assets.Add(asset);
        db.Movements.Add(movement);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        var handler = new ReturnAssignmentCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now.AddDays(1)));
        await handler.Handle(new ReturnAssignmentCommand(assignment.Id, "IT Person", null), CancellationToken.None);

        var reloadedAsset = await db.Assets.SingleAsync(a => a.Id == asset.Id);
        reloadedAsset.Status.Should().Be(AssetStatus.InWarehouse);

        var reloadedAssignment = await db.Assignments.SingleAsync(a => a.Id == assignment.Id);
        reloadedAssignment.Status.Should().Be(AssignmentStatus.Returned);
        reloadedAssignment.ReturnSignatureRecordId.Should().NotBeNull();
    }

    [Fact]
    public async Task Returning_one_member_of_a_group_cascades_to_the_whole_group()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var recipientUserId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var (laptop, laptopMovement, laptopAssignment) = CreateAcceptedAssignment(companyId, recipientUserId, groupId, "ASSET-000001");
        var (charger, chargerMovement, chargerAssignment) = CreateAcceptedAssignment(companyId, recipientUserId, groupId, "ASSET-000002");
        db.Assets.AddRange(laptop, charger);
        db.Movements.AddRange(laptopMovement, chargerMovement);
        db.Assignments.AddRange(laptopAssignment, chargerAssignment);
        await db.SaveChangesAsync();

        var handler = new ReturnAssignmentCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now.AddDays(1)));
        await handler.Handle(new ReturnAssignmentCommand(laptopAssignment.Id, "IT Person", null), CancellationToken.None);

        var reloadedLaptop = await db.Assets.SingleAsync(a => a.Id == laptop.Id);
        var reloadedCharger = await db.Assets.SingleAsync(a => a.Id == charger.Id);
        reloadedLaptop.Status.Should().Be(AssetStatus.InWarehouse);
        reloadedCharger.Status.Should().Be(AssetStatus.InWarehouse, "the accessory returns together with the primary");

        var reloadedChargerAssignment = await db.Assignments.SingleAsync(a => a.Id == chargerAssignment.Id);
        reloadedChargerAssignment.Status.Should().Be(AssignmentStatus.Returned);
        reloadedChargerAssignment.ReturnSignatureRecordId.Should().NotBeNull();
    }
}
