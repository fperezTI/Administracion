using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Users;

public sealed record GetUsersQuery(
    int PageNumber = 1,
    int PageSize = 50,
    bool? IsActive = null,
    /// <summary>displayName (default) | email | isActive | lastLoginAtUtc</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<UserSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Users.Read;
}

public sealed record UserSummary(
    Guid Id, string DisplayName, string Email, bool IsActive, DateTimeOffset? LastLoginAtUtc);

public sealed class GetUsersQueryHandler(IApplicationDbContext db) : IRequestHandler<GetUsersQuery, PagedResult<UserSummary>>
{
    public Task<PagedResult<UserSummary>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking().AsQueryable();

        if (request.IsActive is { } isActive)
        {
            query = query.Where(u => u.IsActive == isActive);
        }

        // LastLoginAtUtc es nullable (nunca inició sesión): se ordena siempre al final sin importar la
        // dirección, mismo criterio ya usado en otras queries con columnas nullable.
        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "email" => descending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "isActive" => descending ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
            "lastLoginAtUtc" => descending
                ? query.OrderBy(u => u.LastLoginAtUtc == null).ThenByDescending(u => u.LastLoginAtUtc)
                : query.OrderBy(u => u.LastLoginAtUtc == null).ThenBy(u => u.LastLoginAtUtc),
            "displayName" => descending ? query.OrderByDescending(u => u.DisplayName) : query.OrderBy(u => u.DisplayName),
            _ => query.OrderBy(u => u.DisplayName),
        };

        var projected = ordered.Select(u => new UserSummary(u.Id, u.DisplayName, u.Email, u.IsActive, u.LastLoginAtUtc));

        return PagedResult<UserSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
