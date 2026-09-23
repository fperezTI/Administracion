using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Roles;

public sealed record GetRolesQuery(
    int PageNumber = 1,
    int PageSize = 50,
    bool? IsActive = null,
    /// <summary>name (default) | description | permissionCount | isActive</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<RoleSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Roles.Read;
}

public sealed record RoleSummary(Guid Id, string Name, string? Description, bool IsActive, int PermissionCount);

public sealed class GetRolesQueryHandler(IApplicationDbContext db) : IRequestHandler<GetRolesQuery, PagedResult<RoleSummary>>
{
    public Task<PagedResult<RoleSummary>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Roles.AsNoTracking().AsQueryable();

        if (request.IsActive is { } isActive)
        {
            query = query.Where(r => r.IsActive == isActive);
        }

        // Description es nullable: se ordena siempre al final sin importar la dirección.
        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "description" => descending
                ? query.OrderBy(r => r.Description == null).ThenByDescending(r => r.Description)
                : query.OrderBy(r => r.Description == null).ThenBy(r => r.Description),
            "permissionCount" => descending
                ? query.OrderByDescending(r => r.RolePermissions.Count)
                : query.OrderBy(r => r.RolePermissions.Count),
            "isActive" => descending ? query.OrderByDescending(r => r.IsActive) : query.OrderBy(r => r.IsActive),
            "name" => descending ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
            _ => query.OrderBy(r => r.Name),
        };

        var projected = ordered.Select(r => new RoleSummary(
            r.Id, r.Name, r.Description, r.IsActive, r.RolePermissions.Count));

        return PagedResult<RoleSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
