using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Permissions;

/// <summary>Reads the seeded permission catalog, grouped by module for the matrix UI. Permissions are
/// never created through the API in V1 — see docs/security/authorization-rbac.md.</summary>
public sealed record GetPermissionsQuery : IRequest<IReadOnlyCollection<PermissionModuleGroup>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Permissions.Read;
}

public sealed record PermissionSummary(Guid Id, string Module, string Action, string Code, string Description);

public sealed record PermissionModuleGroup(string Module, IReadOnlyCollection<PermissionSummary> Permissions);

public sealed class GetPermissionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetPermissionsQuery, IReadOnlyCollection<PermissionModuleGroup>>
{
    public async Task<IReadOnlyCollection<PermissionModuleGroup>> Handle(
        GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await db.Permissions.AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Action)
            .Select(p => new PermissionSummary(p.Id, p.Module, p.Action, p.Module + "." + p.Action, p.Description))
            .ToListAsync(cancellationToken);

        return permissions
            .GroupBy(p => p.Module)
            .Select(g => new PermissionModuleGroup(g.Key, g.ToList()))
            .ToList();
    }
}
