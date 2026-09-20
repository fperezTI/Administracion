using AssetManagement.Domain.Requests;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Requests;

public class InternalRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid RequesterId = Guid.NewGuid();

    private static InternalRequest CreateAssignmentRequest() =>
        InternalRequest.Create(
            Guid.NewGuid(), InternalRequestType.AssetAssignment, Guid.NewGuid(), RequesterId, "Necesito equipo",
            null, Now, RequesterId);

    private static InternalRequest CreateLoanRequest() =>
        InternalRequest.Create(
            Guid.NewGuid(), InternalRequestType.Loan, Guid.NewGuid(), RequesterId, "Préstamo para viaje",
            new DateOnly(2026, 10, 1), Now, RequesterId);

    [Fact]
    public void Create_starts_pending_approval()
    {
        var request = CreateAssignmentRequest();

        request.Status.Should().Be(InternalRequestStatus.PendingApproval);
    }

    [Fact]
    public void Create_rejects_empty_justification()
    {
        var act = () => InternalRequest.Create(
            Guid.NewGuid(), InternalRequestType.AssetAssignment, Guid.NewGuid(), RequesterId, "  ", null, Now, RequesterId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_a_loan_request_without_expected_return_date_throws()
    {
        var act = () => InternalRequest.Create(
            Guid.NewGuid(), InternalRequestType.Loan, Guid.NewGuid(), RequesterId, "Préstamo", null, Now, RequesterId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_a_non_loan_request_with_expected_return_date_throws()
    {
        var act = () => InternalRequest.Create(
            Guid.NewGuid(), InternalRequestType.AssetAssignment, Guid.NewGuid(), RequesterId, "Equipo",
            new DateOnly(2026, 10, 1), Now, RequesterId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_a_loan_request_with_expected_return_date_succeeds()
    {
        var request = CreateLoanRequest();

        request.ExpectedReturnDate.Should().Be(new DateOnly(2026, 10, 1));
    }

    [Fact]
    public void Reject_a_pending_request_succeeds()
    {
        var request = CreateAssignmentRequest();

        request.Reject(Now, null);

        request.Status.Should().Be(InternalRequestStatus.Rejected);
    }

    [Fact]
    public void Reject_twice_throws()
    {
        var request = CreateAssignmentRequest();
        request.Reject(Now, null);

        var act = () => request.Reject(Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_by_the_requester_succeeds()
    {
        var request = CreateAssignmentRequest();

        request.Cancel(RequesterId, Now, RequesterId);

        request.Status.Should().Be(InternalRequestStatus.Cancelled);
    }

    [Fact]
    public void Cancel_by_someone_other_than_the_requester_throws()
    {
        var request = CreateAssignmentRequest();

        var act = () => request.Cancel(Guid.NewGuid(), Now, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Fulfill_a_pending_request_succeeds()
    {
        var request = CreateAssignmentRequest();
        var referenceId = Guid.NewGuid();

        request.Fulfill(referenceId, Now, null);

        request.Status.Should().Be(InternalRequestStatus.Fulfilled);
        request.FulfillmentReferenceId.Should().Be(referenceId);
    }

    [Fact]
    public void Fulfill_an_already_decided_request_throws()
    {
        var request = CreateAssignmentRequest();
        request.Cancel(RequesterId, Now, RequesterId);

        var act = () => request.Fulfill(Guid.NewGuid(), Now, null);

        act.Should().Throw<DomainException>();
    }
}
