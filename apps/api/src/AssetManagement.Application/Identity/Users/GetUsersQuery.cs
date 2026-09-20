using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Users;

public sealed record GetUsersQuery(int PageNumber = 1, int PageSize = 50, bool? IsActive = null)
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
        var query = db.Users.AsNoTracking().OrderBy(u => u.DisplayName).AsQueryable();

        if (request.IsActive is { } isActive)
        {
            query = query.Where(u => u.IsActive == isActive);
        }

        var projected = query.Select(u => new UserSummary(u.Id, u.DisplayName, u.Email, u.IsActive, u.LastLoginAtUtc));

        return PagedResult<UserSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
