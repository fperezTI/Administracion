using AssetManagement.Domain.SharedKernel;
using AssetManagement.Domain.Signature;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Signature;

public class SignatureRecordTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_captures_a_typed_confirmation_without_an_image()
    {
        var record = SignatureRecord.Create(
            Guid.NewGuid(), "AssignmentAcceptance", Guid.NewGuid(), Guid.NewGuid(), "Ada Lovelace",
            "127.0.0.1", "TestAgent/1.0", "ABCDEF", SignatureRecord.TypedConfirmationMechanism, null, Now, null);

        record.Mechanism.Should().Be(SignatureRecord.TypedConfirmationMechanism);
        record.SignatureImageDataUrl.Should().BeNull();
        record.SignedAtUtc.Should().Be(Now);
    }

    [Fact]
    public void Create_captures_a_drawn_signature_with_its_image()
    {
        var record = SignatureRecord.Create(
            Guid.NewGuid(), "ApprovalDecision", Guid.NewGuid(), Guid.NewGuid(), "Ada Lovelace",
            "127.0.0.1", "TestAgent/1.0", "ABCDEF", SignatureRecord.DrawnSignatureMechanism,
            "data:image/png;base64,iVBORw0KGgo=", Now, null);

        record.Mechanism.Should().Be(SignatureRecord.DrawnSignatureMechanism);
        record.SignatureImageDataUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_rejects_a_drawn_signature_without_an_image()
    {
        var act = () => SignatureRecord.Create(
            Guid.NewGuid(), "ApprovalDecision", Guid.NewGuid(), Guid.NewGuid(), "Ada Lovelace",
            null, null, "ABCDEF", SignatureRecord.DrawnSignatureMechanism, null, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_a_typed_confirmation_carrying_an_image()
    {
        var act = () => SignatureRecord.Create(
            Guid.NewGuid(), "AssignmentAcceptance", Guid.NewGuid(), Guid.NewGuid(), "Ada Lovelace",
            null, null, "ABCDEF", SignatureRecord.TypedConfirmationMechanism,
            "data:image/png;base64,iVBORw0KGgo=", Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_an_unrecognized_mechanism()
    {
        var act = () => SignatureRecord.Create(
            Guid.NewGuid(), "AssignmentAcceptance", Guid.NewGuid(), Guid.NewGuid(), "Ada Lovelace",
            null, null, "ABCDEF", "Fingerprint", null, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_an_empty_signer_name()
    {
        var act = () => SignatureRecord.Create(
            Guid.NewGuid(), "AssignmentAcceptance", Guid.NewGuid(), Guid.NewGuid(), "  ",
            null, null, "ABCDEF", SignatureRecord.TypedConfirmationMechanism, null, Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_an_empty_content_hash()
    {
        var act = () => SignatureRecord.Create(
            Guid.NewGuid(), "AssignmentAcceptance", Guid.NewGuid(), Guid.NewGuid(), "Ada Lovelace",
            null, null, "", SignatureRecord.TypedConfirmationMechanism, null, Now, null);

        act.Should().Throw<DomainException>();
    }
}
