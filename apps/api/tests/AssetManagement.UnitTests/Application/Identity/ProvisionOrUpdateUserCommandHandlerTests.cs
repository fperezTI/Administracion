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
        snapshot.PermissionCodes.Should().BeEmpty();
        (await db.Users.CountAsync()).Should().Be(1);
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
    }
}
