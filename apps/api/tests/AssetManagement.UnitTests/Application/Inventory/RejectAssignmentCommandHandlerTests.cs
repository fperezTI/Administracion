using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Inventory;

public class RejectAssignmentCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private static (Asset Asset, Movement Movement, Assignment Assignment) CreatePendingAssignment(
        Guid companyId, Guid recipientUserId, Guid? groupId, string internalFolio, Guid? createdByUserId = null)
    {
        var asset = Asset.Create(
            companyId, Guid.NewGuid(), internalFolio, "Dell", "Latitude 5450", null, null,
            PhysicalCondition.Excellent, Now, null);
        asset.ChangeStatus(AssetStatus.Reserved, Now, null);
        var movement = Movement.Create(
            companyId, asset.Id, MovementType.Assignment, $"MOV-{internalFolio}", startsCompleted: false,
            fromOrgUnitId: null, toOrgUnitId: null, fromUserId: null, toUserId: recipientUserId, notes: null,
            Now, createdByUserId);
        var assignment = Assignment.Create(companyId, asset.Id, recipientUserId, null, movement.Id, Now, createdByUserId, groupId);
        return (asset, movement, assignment);
    }

    [Fact]
    public async Task Rejecting_by_the_named_recipient_cancels_the_assignment_and_frees_the_asset()
    {
        var companyId = Guid.NewGuid();
        var recipientUserId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var (asset, movement, assignment) = CreatePendingAssignment(companyId, recipientUserId, null, "ASSET-000001");
        db.Assets.Add(asset);
        db.Movements.Add(movement);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        var handler = new RejectAssignmentCommandHandler(
            db, new FakeCurrentUserContext { UserId = recipientUserId }, new FakeClock(Now),
            new FakeNotificationSender(), new FakeFrontendLinkBuilder());
        await handler.Handle(new RejectAssignmentCommand(assignment.Id), CancellationToken.None);

        var reloadedAsset = await db.Assets.SingleAsync(a => a.Id == asset.Id);
        reloadedAsset.Status.Should().Be(AssetStatus.InWarehouse);

        var reloadedAssignment = await db.Assignments.SingleAsync(a => a.Id == assignment.Id);
        reloadedAssignment.Status.Should().Be(AssignmentStatus.Cancelled);
    }

    [Fact]
    public async Task Rejecting_one_member_of_a_group_cancels_the_whole_group()
    {
        var companyId = Guid.NewGuid();
        var recipientUserId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        using var db = InMemoryAppDbContextFactory.Create(new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] });
        var (laptop, laptopMovement, laptopAssignment) = CreatePendingAssignment(companyId, recipientUserId, groupId, "ASSET-000001");
        var (charger, chargerMovement, chargerAssignment) = CreatePendingAssignment(companyId, recipientUserId, groupId, "ASSET-000002");
        db.Assets.AddRange(laptop, charger);
        db.Movements.AddRange(laptopMovement, chargerMovement);
        db.Assignments.AddRange(laptopAssignment, chargerAssignment);
        await db.SaveChangesAsync();

        var handler = new RejectAssignmentCommandHandler(
            db, new FakeCurrentUserContext { UserId = recipientUserId }, new FakeClock(Now),
            new FakeNotificationSender(), new FakeFrontendLinkBuilder());
        await handler.Handle(new RejectAssignmentCommand(laptopAssignment.Id), CancellationToken.None);

        var reloadedCharger = await db.Assets.SingleAsync(a => a.Id == charger.Id);
        reloadedCharger.Status.Should().Be(AssetStatus.InWarehouse, "rejecting the primary rejects the whole group");

        var reloadedChargerAssignment = await db.Assignments.SingleAsync(a => a.Id == chargerAssignment.Id);
        reloadedChargerAssignment.Status.Should().Be(AssignmentStatus.Cancelled);
    }

    [Fact]
    public async Task Rejecting_by_someone_other_than_the_recipient_is_forbidden()
    {
        var companyId = Guid.NewGuid();
        var recipientUserId = Guid.NewGuid();
        using var db = InMemoryAppDbContextFactory.Create(new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] });
        var (asset, movement, assignment) = CreatePendingAssignment(companyId, recipientUserId, null, "ASSET-000001");
        db.Assets.Add(asset);
        db.Movements.Add(movement);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        var handler = new RejectAssignmentCommandHandler(
            db, new FakeCurrentUserContext { UserId = Guid.NewGuid() }, new FakeClock(Now),
            new FakeNotificationSender(), new FakeFrontendLinkBuilder());
        var act = () => handler.Handle(new RejectAssignmentCommand(assignment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Rejecting_notifies_whoever_created_the_assignment()
    {
        var companyId = Guid.NewGuid();
        var recipientUserId = Guid.NewGuid();
        var creatorUserId = Guid.NewGuid();
        using var db = InMemoryAppDbContextFactory.Create(new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] });
        var (asset, movement, assignment) = CreatePendingAssignment(companyId, recipientUserId, null, "ASSET-000001", creatorUserId);
        db.Assets.Add(asset);
        db.Movements.Add(movement);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        var notificationSender = new FakeNotificationSender();
        var handler = new RejectAssignmentCommandHandler(
            db, new FakeCurrentUserContext { UserId = recipientUserId }, new FakeClock(Now),
            notificationSender, new FakeFrontendLinkBuilder());
        await handler.Handle(new RejectAssignmentCommand(assignment.Id), CancellationToken.None);

        notificationSender.Notifications.Should().ContainSingle();
        var notification = notificationSender.Notifications.Single();
        notification.UserId.Should().Be(creatorUserId);
        notification.Type.Should().Be("AssignmentRejected");
    }

    [Fact]
    public async Task Rejecting_an_assignment_with_no_recorded_creator_does_not_notify_or_throw()
    {
        var companyId = Guid.NewGuid();
        var recipientUserId = Guid.NewGuid();
        using var db = InMemoryAppDbContextFactory.Create(new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] });
        var (asset, movement, assignment) = CreatePendingAssignment(companyId, recipientUserId, null, "ASSET-000001", createdByUserId: null);
        db.Assets.Add(asset);
        db.Movements.Add(movement);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        var notificationSender = new FakeNotificationSender();
        var handler = new RejectAssignmentCommandHandler(
            db, new FakeCurrentUserContext { UserId = recipientUserId }, new FakeClock(Now),
            notificationSender, new FakeFrontendLinkBuilder());
        await handler.Handle(new RejectAssignmentCommand(assignment.Id), CancellationToken.None);

        notificationSender.Notifications.Should().BeEmpty();
    }
}
