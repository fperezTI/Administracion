using System.Text.Json;
using AssetManagement.Application.SystemConfiguration;
using AssetManagement.Domain.Configuration;
using AssetManagement.Domain.SharedKernel;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.SystemConfiguration;

public class UpdateSystemSettingsCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Creates_the_singleton_row_on_first_save()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var currentUser = new FakeCurrentUserContext();
        var handler = new UpdateSystemSettingsCommandHandler(db, new FakeSecretProtector(), currentUser, new FakeClock(Now));

        await handler.Handle(
            new UpdateSystemSettingsCommand("notificaciones@empresa.com", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "s3cr3t"),
            CancellationToken.None);

        var settings = await db.SystemSettings.FindAsync(SystemSettings.SingletonId);
        settings.Should().NotBeNull();
        settings!.SenderMailbox.Should().Be("notificaciones@empresa.com");
        settings.GraphClientSecretCiphertext.Should().Be("protected:s3cr3t");
        settings.UpdatedByUserId.Should().Be(currentUser.UserId);
    }

    [Fact]
    public async Task Encrypts_the_secret_before_storing_it()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var handler = new UpdateSystemSettingsCommandHandler(
            db, new FakeSecretProtector(), new FakeCurrentUserContext(), new FakeClock(Now));

        await handler.Handle(
            new UpdateSystemSettingsCommand(null, null, null, "super-secret-value"),
            CancellationToken.None);

        var settings = await db.SystemSettings.FindAsync(SystemSettings.SingletonId);
        settings!.GraphClientSecretCiphertext.Should().NotBe("super-secret-value");
        settings.GraphClientSecretCiphertext.Should().Contain("super-secret-value"); // fake protector only prefixes
    }

    [Fact]
    public async Task Leaving_the_secret_blank_keeps_the_previously_stored_one()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var handler = new UpdateSystemSettingsCommandHandler(
            db, new FakeSecretProtector(), new FakeCurrentUserContext(), new FakeClock(Now));

        await handler.Handle(new UpdateSystemSettingsCommand(null, null, null, "original-secret"), CancellationToken.None);
        await handler.Handle(new UpdateSystemSettingsCommand("nuevo@empresa.com", null, null, null), CancellationToken.None);

        var settings = await db.SystemSettings.FindAsync(SystemSettings.SingletonId);
        settings!.SenderMailbox.Should().Be("nuevo@empresa.com");
        settings.GraphClientSecretCiphertext.Should().Be("protected:original-secret");
    }

    [Fact]
    public async Task Rejects_a_malformed_sender_mailbox()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var handler = new UpdateSystemSettingsCommandHandler(
            db, new FakeSecretProtector(), new FakeCurrentUserContext(), new FakeClock(Now));

        var act = () => handler.Handle(new UpdateSystemSettingsCommand("not-an-email", null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Rejects_a_malformed_tenant_id()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var handler = new UpdateSystemSettingsCommandHandler(
            db, new FakeSecretProtector(), new FakeCurrentUserContext(), new FakeClock(Now));

        var act = () => handler.Handle(new UpdateSystemSettingsCommand(null, "not-a-guid", null, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public void The_redacted_audit_json_never_contains_the_plaintext_secret()
    {
        var command = new UpdateSystemSettingsCommand("mailbox@empresa.com", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "top-secret-value");

        var redactedJson = command.ToRedactedAuditJson();

        redactedJson.Should().NotContain("top-secret-value");
        redactedJson.Should().Contain("\"GraphClientSecretProvided\":true");
    }

    [Fact]
    public void The_secret_survives_a_normal_JSON_round_trip_so_model_binding_actually_receives_it()
    {
        // Regression test: an earlier version marked GraphClientSecret [JsonIgnore] to keep it out of the
        // audit trail, which also made ASP.NET Core's model binder silently drop it on every real PUT
        // request — the secret could never actually be saved. See IRedactsAuditDetails instead.
        var command = new UpdateSystemSettingsCommand("mailbox@empresa.com", null, null, "top-secret-value");

        var json = JsonSerializer.Serialize(command, command.GetType());
        var roundTripped = JsonSerializer.Deserialize<UpdateSystemSettingsCommand>(json);

        roundTripped!.GraphClientSecret.Should().Be("top-secret-value");
    }
}
