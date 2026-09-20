using AssetManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Security;

public sealed class EfPermissionChecker(IApplicationDbContext db) : IPermissionChecker
{
    public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken)
    {
        return (
            from userRole in db.UserRoles
            join role in db.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId && role.IsActive
            join rolePermission in db.RolePermissions on role.Id equals rolePermission.RoleId
            join permission in db.Permissions on rolePermission.PermissionId equals permission.Id
            where permission.Module + "." + permission.Action == permissionCode
            select 1)
            .AnyAsync(cancellationToken);
    }
}
