using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Roles;

public sealed record GetRolesQuery(int PageNumber = 1, int PageSize = 50, bool? IsActive = null)
    : IRequest<PagedResult<RoleSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Roles.Read;
}

public sealed record RoleSummary(Guid Id, string Name, string? Description, bool IsActive, int PermissionCount);

public sealed class GetRolesQueryHandler(IApplicationDbContext db) : IRequestHandler<GetRolesQuery, PagedResult<RoleSummary>>
{
    public Task<PagedResult<RoleSummary>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Roles.AsNoTracking().OrderBy(r => r.Name).AsQueryable();

        if (request.IsActive is { } isActive)
        {
            query = query.Where(r => r.IsActive == isActive);
        }

        var projected = query.Select(r => new RoleSummary(
            r.Id, r.Name, r.Description, r.IsActive, r.RolePermissions.Count));

        return PagedResult<RoleSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
