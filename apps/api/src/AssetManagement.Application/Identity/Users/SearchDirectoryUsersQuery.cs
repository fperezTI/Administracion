using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Users;

/// <summary>Searches the tenant directory for people to add via <see cref="CreateUserFromDirectoryCommand"/>
/// — excludes anyone who already has a local profile, since this screen exists to bring in new people, not
/// to manage existing ones (that's `/users/{id}`).</summary>
public sealed record SearchDirectoryUsersQuery(string Query) : IRequest<IReadOnlyList<DirectoryUserResult>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Users.Create;
}

public sealed record DirectoryUserResult(Guid EntraObjectId, string DisplayName, string Email);

public sealed class SearchDirectoryUsersQueryHandler(IApplicationDbContext db, IDirectoryUserSearch directoryUserSearch)
    : IRequestHandler<SearchDirectoryUsersQuery, IReadOnlyList<DirectoryUserResult>>
{
    public async Task<IReadOnlyList<DirectoryUserResult>> Handle(
        SearchDirectoryUsersQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return [];
        }

        var candidates = await directoryUserSearch.SearchAsync(request.Query.Trim(), cancellationToken);
        if (candidates.Count == 0)
        {
            return [];
        }

        var candidateIds = candidates.Select(c => c.EntraObjectId).ToList();
        var existingEntraObjectIds = await db.Users
            .Where(u => candidateIds.Contains(u.EntraObjectId))
            .Select(u => u.EntraObjectId)
            .ToListAsync(cancellationToken);
        var existingSet = existingEntraObjectIds.ToHashSet();

        return candidates
            .Where(c => !existingSet.Contains(c.EntraObjectId))
            .Select(c => new DirectoryUserResult(c.EntraObjectId, c.DisplayName, c.Email))
            .ToList();
    }
}
