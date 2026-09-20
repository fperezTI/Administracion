using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Identity.Users;
using AssetManagement.Domain.Identity;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Identity;

public class AnonymizeUserCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Anonymizes_the_user_and_persists_the_change()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new AnonymizeUserCommandHandler(db, new FakeClock(Now));
        await handler.Handle(new AnonymizeUserCommand(user.Id), CancellationToken.None);

        var reloaded = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        reloaded.DisplayName.Should().NotBe("Ada Lovelace");
        reloaded.Email.Should().NotBe("ada@example.com");
        reloaded.IsActive.Should().BeFalse();
        reloaded.AnonymizedAtUtc.Should().Be(Now);
    }

    [Fact]
    public async Task Unknown_user_throws_NotFoundException()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var handler = new AnonymizeUserCommandHandler(db, new FakeClock(Now));

        var act = () => handler.Handle(new AnonymizeUserCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
