using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Identity.Users;
using AssetManagement.Domain.Identity;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.Identity.Users;

public class SearchDirectoryUsersQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Excludes_directory_results_that_already_have_a_local_profile()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var existingEntraObjectId = Guid.NewGuid();
        var newEntraObjectId = Guid.NewGuid();
        db.Users.Add(User.Provision(existingEntraObjectId, "Ada Lovelace", "ada@example.com", Now));
        await db.SaveChangesAsync();

        var directorySearch = new FakeDirectoryUserSearch
        {
            Results =
            [
                new DirectoryUser(existingEntraObjectId, "Ada Lovelace", "ada@example.com"),
                new DirectoryUser(newEntraObjectId, "Grace Hopper", "grace@example.com"),
            ],
        };
        var handler = new SearchDirectoryUsersQueryHandler(db, directorySearch);

        var results = await handler.Handle(new SearchDirectoryUsersQuery("grace"), CancellationToken.None);

        results.Should().ContainSingle(r => r.EntraObjectId == newEntraObjectId);
    }

    [Fact]
    public async Task Returns_empty_for_a_blank_query_without_calling_the_directory()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var directorySearch = new FakeDirectoryUserSearch
        {
            Results = [new DirectoryUser(Guid.NewGuid(), "Grace Hopper", "grace@example.com")],
        };
        var handler = new SearchDirectoryUsersQueryHandler(db, directorySearch);

        var results = await handler.Handle(new SearchDirectoryUsersQuery("   "), CancellationToken.None);

        results.Should().BeEmpty();
    }
}
