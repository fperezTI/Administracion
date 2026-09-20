using AssetManagement.Domain.Inventory;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Inventory;

public class AssignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    private static Assignment CreateAssignment() =>
        Assignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), Now, null);

    [Fact]
    public void Create_starts_pending_signature()
    {
        var assignment = CreateAssignment();

        assignment.Status.Should().Be(AssignmentStatus.PendingSignature);
    }

    [Fact]
    public void Accept_moves_to_accepted_and_records_the_signature()
    {
        var assignment = CreateAssignment();
        var signatureId = Guid.NewGuid();

        assignment.Accept(signatureId, Now, null);

        assignment.Status.Should().Be(AssignmentStatus.Accepted);
        assignment.SignatureRecordId.Should().Be(signatureId);
        assignment.AcceptedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Accept_twice_throws()
    {
        var assignment = CreateAssignment();
        assignment.Accept(Guid.NewGuid(), Now, null);

        var act = () => assignment.Accept(Guid.NewGuid(), Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_a_pending_assignment_succeeds()
    {
        var assignment = CreateAssignment();

        assignment.Cancel(Now, null);

        assignment.Status.Should().Be(AssignmentStatus.Cancelled);
    }

    [Fact]
    public void Cancel_an_accepted_assignment_throws()
    {
        var assignment = CreateAssignment();
        assignment.Accept(Guid.NewGuid(), Now, null);

        var act = () => assignment.Cancel(Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Return_before_acceptance_throws()
    {
        var assignment = CreateAssignment();

        var act = () => assignment.Return(Guid.NewGuid(), Guid.NewGuid(), Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Return_after_acceptance_succeeds()
    {
        var assignment = CreateAssignment();
        assignment.Accept(Guid.NewGuid(), Now, null);
        var returnSignatureId = Guid.NewGuid();
        var returnMovementId = Guid.NewGuid();

        assignment.Return(returnSignatureId, returnMovementId, Now.AddDays(30), null);

        assignment.Status.Should().Be(AssignmentStatus.Returned);
        assignment.ReturnSignatureRecordId.Should().Be(returnSignatureId);
        assignment.ReturnMovementId.Should().Be(returnMovementId);
    }
}
