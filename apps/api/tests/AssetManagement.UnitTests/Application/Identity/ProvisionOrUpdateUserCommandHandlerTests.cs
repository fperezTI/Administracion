using AssetManagement.Application.Identity;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Identity;

public class ProvisionOrUpdateUserCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task First_login_creates_a_local_user_profile()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var handler = new ProvisionOrUpdateUserCommandHandler(db, new FakeClock(Now));
        var entraObjectId = Guid.NewGuid();

        var snapshot = await handler.Handle(
            new ProvisionOrUpdateUserCommand(entraObjectId, "Ada Lovelace", "ada@example.com"), CancellationToken.None);

        snapshot.EntraObjectId.Should().Be(entraObjectId);
        snapshot.IsActive.Should().BeTrue();
        (await db.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task First_user_ever_is_bootstrapped_as_super_admin_with_every_permission()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var handler = new ProvisionOrUpdateUserCommandHandler(db, new FakeClock(Now));
        var allPermissionCodes = await db.Permissions.Select(p => p.Module + "." + p.Action).ToListAsync();

        var snapshot = await handler.Handle(
            new ProvisionOrUpdateUserCommand(Guid.NewGuid(), "Ada Lovelace", "ada@example.com"), CancellationToken.None);

        snapshot.PermissionCodes.Should().BeEquivalentTo(allPermissionCodes);
        (await db.Roles.CountAsync()).Should().Be(1);
        (await db.Roles.SingleAsync()).Name.Should().Be("Super Administrador");
        (await db.AuditEntries.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Second_distinct_user_does_not_receive_super_admin_permissions()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        await new ProvisionOrUpdateUserCommandHandler(db, new FakeClock(Now)).Handle(
            new ProvisionOrUpdateUserCommand(Guid.NewGuid(), "Ada Lovelace", "ada@example.com"), CancellationToken.None);

        var secondSnapshot = await new ProvisionOrUpdateUserCommandHandler(db, new FakeClock(Now)).Handle(
            new ProvisionOrUpdateUserCommand(Guid.NewGuid(), "Grace Hopper", "grace@example.com"), CancellationToken.None);

        secondSnapshot.PermissionCodes.Should().BeEmpty();
        (await db.Roles.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Second_login_updates_the_same_profile_instead_of_creating_a_duplicate()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var entraObjectId = Guid.NewGuid();
        var firstHandler = new ProvisionOrUpdateUserCommandHandler(db, new FakeClock(Now));
        var firstSnapshot = await firstHandler.Handle(
            new ProvisionOrUpdateUserCommand(entraObjectId, "Ada Lovelace", "ada@old.example.com"), CancellationToken.None);

        var later = Now.AddDays(1);
        var secondHandler = new ProvisionOrUpdateUserCommandHandler(db, new FakeClock(later));
        var secondSnapshot = await secondHandler.Handle(
            new ProvisionOrUpdateUserCommand(entraObjectId, "Ada Lovelace", "ada@new.example.com"), CancellationToken.None);

        secondSnapshot.UserId.Should().Be(firstSnapshot.UserId);
        secondSnapshot.Email.Should().Be("ada@new.example.com");
        (await db.Users.CountAsync()).Should().Be(1);
        (await db.Users.SingleAsync()).LastLoginAtUtc.Should().Be(later);
        (await db.Roles.CountAsync()).Should().Be(1);
        (await db.AuditEntries.CountAsync()).Should().Be(1);
    }
}
