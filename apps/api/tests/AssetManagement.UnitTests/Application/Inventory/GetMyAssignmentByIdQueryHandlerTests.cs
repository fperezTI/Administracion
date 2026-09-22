using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Inventory;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.Inventory;

public class GetMyAssignmentByIdQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private static (Asset Asset, Movement Movement, Assignment Assignment, User Recipient) CreatePendingAssignment(Guid companyId)
    {
        var recipient = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        var asset = Asset.Create(
            companyId, Guid.NewGuid(), "ASSET-000001", "Dell", "Latitude 5450", null, null,
            PhysicalCondition.Excellent, Now, null);
        asset.ChangeStatus(AssetStatus.Reserved, Now, null);
        var movement = Movement.Create(
            companyId, asset.Id, MovementType.Assignment, "MOV-ASG-000001", startsCompleted: false,
            null, null, null, recipient.Id, null, Now, null);
        var assignment = Assignment.Create(companyId, asset.Id, recipient.Id, null, movement.Id, Now, null);
        return (asset, movement, assignment, recipient);
    }

    [Fact]
    public async Task The_recipient_can_see_their_own_assignment()
    {
        var companyId = Guid.NewGuid();
        using var db = InMemoryAppDbContextFactory.Create(new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] });
        var (asset, movement, assignment, recipient) = CreatePendingAssignment(companyId);
        db.Assets.Add(asset);
        db.Movements.Add(movement);
        db.Assignments.Add(assignment);
        db.Users.Add(recipient);
        await db.SaveChangesAsync();

        var handler = new GetMyAssignmentByIdQueryHandler(db, new FakeCurrentUserContext { UserId = recipient.Id });
        var result = await handler.Handle(new GetMyAssignmentByIdQuery(assignment.Id), CancellationToken.None);

        result.Id.Should().Be(assignment.Id);
        result.AssetFolio.Should().Be("ASSET-000001");
    }

    [Fact]
    public async Task Someone_else_cannot_see_it()
    {
        var companyId = Guid.NewGuid();
        using var db = InMemoryAppDbContextFactory.Create(new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] });
        var (asset, movement, assignment, recipient) = CreatePendingAssignment(companyId);
        db.Assets.Add(asset);
        db.Movements.Add(movement);
        db.Assignments.Add(assignment);
        db.Users.Add(recipient);
        await db.SaveChangesAsync();

        var handler = new GetMyAssignmentByIdQueryHandler(db, new FakeCurrentUserContext { UserId = Guid.NewGuid() });
        var act = () => handler.Handle(new GetMyAssignmentByIdQuery(assignment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task A_nonexistent_assignment_is_not_found()
    {
        using var db = InMemoryAppDbContextFactory.Create();

        var handler = new GetMyAssignmentByIdQueryHandler(db, new FakeCurrentUserContext());
        var act = () => handler.Handle(new GetMyAssignmentByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
