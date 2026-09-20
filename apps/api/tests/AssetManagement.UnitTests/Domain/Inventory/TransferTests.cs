using AssetManagement.Domain.Inventory;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Inventory;

public class TransferTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid RequesterId = Guid.NewGuid();

    private static Transfer CreateTransfer() =>
        Transfer.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), RequesterId, "Consolidación de equipo", Now, RequesterId);

    [Fact]
    public void Create_rejects_the_same_company_as_source_and_destination()
    {
        var companyId = Guid.NewGuid();

        var act = () => Transfer.Create(Guid.NewGuid(), companyId, companyId, RequesterId, null, Now, RequesterId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_starts_pending_approval()
    {
        var transfer = CreateTransfer();

        transfer.Status.Should().Be(TransferStatus.PendingApproval);
    }

    [Fact]
    public void Depart_moves_to_InTransit_and_records_the_movement()
    {
        var transfer = CreateTransfer();
        var movementId = Guid.NewGuid();

        transfer.Depart(movementId, Now, null);

        transfer.Status.Should().Be(TransferStatus.InTransit);
        transfer.DepartureMovementId.Should().Be(movementId);
        transfer.DepartedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Depart_twice_throws()
    {
        var transfer = CreateTransfer();
        transfer.Depart(Guid.NewGuid(), Now, null);

        var act = () => transfer.Depart(Guid.NewGuid(), Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reject_a_pending_transfer_succeeds()
    {
        var transfer = CreateTransfer();

        transfer.Reject(Now, null);

        transfer.Status.Should().Be(TransferStatus.Rejected);
    }

    [Fact]
    public void Reject_an_in_transit_transfer_throws()
    {
        var transfer = CreateTransfer();
        transfer.Depart(Guid.NewGuid(), Now, null);

        var act = () => transfer.Reject(Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_by_the_requester_succeeds_while_pending()
    {
        var transfer = CreateTransfer();

        transfer.Cancel(RequesterId, Now, RequesterId);

        transfer.Status.Should().Be(TransferStatus.Cancelled);
    }

    [Fact]
    public void Cancel_by_someone_other_than_the_requester_throws()
    {
        var transfer = CreateTransfer();

        var act = () => transfer.Cancel(Guid.NewGuid(), Now, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_after_departure_throws()
    {
        var transfer = CreateTransfer();
        transfer.Depart(Guid.NewGuid(), Now, null);

        var act = () => transfer.Cancel(RequesterId, Now, RequesterId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Receive_before_departure_throws()
    {
        var transfer = CreateTransfer();

        var act = () => transfer.Receive(Guid.NewGuid(), Guid.NewGuid(), Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Receive_after_departure_completes_the_transfer()
    {
        var transfer = CreateTransfer();
        transfer.Depart(Guid.NewGuid(), Now, null);
        var receiptMovementId = Guid.NewGuid();
        var signatureId = Guid.NewGuid();

        transfer.Receive(receiptMovementId, signatureId, Now.AddDays(1), null);

        transfer.Status.Should().Be(TransferStatus.Completed);
        transfer.ReceiptMovementId.Should().Be(receiptMovementId);
        transfer.ReceiptSignatureRecordId.Should().Be(signatureId);
        transfer.CompletedAtUtc.Should().Be(Now.AddDays(1));
    }
}
