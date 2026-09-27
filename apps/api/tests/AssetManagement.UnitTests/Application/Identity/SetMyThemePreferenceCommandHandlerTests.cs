using AssetManagement.Application.Identity;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.SharedKernel;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Identity;

public class SetMyThemePreferenceCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Sets_the_callers_own_theme_preference()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);
        var currentUser = new FakeCurrentUserContext { UserId = user.Id };
        var handler = new SetMyThemePreferenceCommandHandler(db, currentUser);

        await handler.Handle(new SetMyThemePreferenceCommand("dark"), CancellationToken.None);

        (await db.Users.SingleAsync()).ThemePreferenceCode.Should().Be("dark");
    }

    [Fact]
    public async Task Null_theme_code_restores_inheritance_from_the_company()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        user.SetThemePreference("dark");
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);
        var currentUser = new FakeCurrentUserContext { UserId = user.Id };
        var handler = new SetMyThemePreferenceCommandHandler(db, currentUser);

        await handler.Handle(new SetMyThemePreferenceCommand(null), CancellationToken.None);

        (await db.Users.SingleAsync()).ThemePreferenceCode.Should().BeNull();
    }

    [Fact]
    public async Task Rejects_an_unknown_theme_code()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);
        var currentUser = new FakeCurrentUserContext { UserId = user.Id };
        var handler = new SetMyThemePreferenceCommandHandler(db, currentUser);

        var act = () => handler.Handle(new SetMyThemePreferenceCommand("neon-pink"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Setting_the_same_theme_twice_is_safe()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);
        var currentUser = new FakeCurrentUserContext { UserId = user.Id };
        var handler = new SetMyThemePreferenceCommandHandler(db, currentUser);

        await handler.Handle(new SetMyThemePreferenceCommand("dark"), CancellationToken.None);
        await handler.Handle(new SetMyThemePreferenceCommand("dark"), CancellationToken.None);

        (await db.Users.SingleAsync()).ThemePreferenceCode.Should().Be("dark");
    }

    [Fact]
    public async Task Never_touches_another_users_preference()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var caller = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        var otherUser = User.Provision(Guid.NewGuid(), "Grace Hopper", "grace@example.com", Now);
        db.Users.AddRange(caller, otherUser);
        await db.SaveChangesAsync(CancellationToken.None);
        var currentUser = new FakeCurrentUserContext { UserId = caller.Id };
        var handler = new SetMyThemePreferenceCommandHandler(db, currentUser);

        await handler.Handle(new SetMyThemePreferenceCommand("dark"), CancellationToken.None);

        (await db.Users.SingleAsync(u => u.Id == otherUser.Id)).ThemePreferenceCode.Should().BeNull();
    }
}
