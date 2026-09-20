using AssetManagement.Domain.Inventory;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Inventory;

public class MovementTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_starts_completed_when_requested()
    {
        var movement = Movement.Create(
            Guid.NewGuid(), Guid.NewGuid(), MovementType.Loan, "MOV-LOAN-000001", startsCompleted: true,
            null, null, null, Guid.NewGuid(), null, Now, null);

        movement.Status.Should().Be(MovementStatus.Completed);
    }

    [Fact]
    public void Create_starts_pending_when_not_completed_immediately()
    {
        var movement = Movement.Create(
            Guid.NewGuid(), Guid.NewGuid(), MovementType.Assignment, "MOV-ASG-000001", startsCompleted: false,
            null, null, null, Guid.NewGuid(), null, Now, null);

        movement.Status.Should().Be(MovementStatus.Pending);
    }

    [Fact]
    public void Complete_on_an_already_completed_movement_throws()
    {
        var movement = Movement.Create(
            Guid.NewGuid(), Guid.NewGuid(), MovementType.Loan, "MOV-LOAN-000001", startsCompleted: true,
            null, null, null, null, null, Now, null);

        var act = () => movement.Complete(Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_on_a_pending_movement_succeeds()
    {
        var movement = Movement.Create(
            Guid.NewGuid(), Guid.NewGuid(), MovementType.Assignment, "MOV-ASG-000001", startsCompleted: false,
            null, null, null, null, null, Now, null);

        movement.Cancel(Now, null);

        movement.Status.Should().Be(MovementStatus.Cancelled);
    }

    [Fact]
    public void Cancel_on_a_completed_movement_throws()
    {
        var movement = Movement.Create(
            Guid.NewGuid(), Guid.NewGuid(), MovementType.Loan, "MOV-LOAN-000001", startsCompleted: true,
            null, null, null, null, null, Now, null);

        var act = () => movement.Cancel(Now, null);

        act.Should().Throw<DomainException>();
    }
}
