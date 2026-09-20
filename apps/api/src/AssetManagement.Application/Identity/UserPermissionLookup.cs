using AssetManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity;

/// <summary>Shared query used by provisioning, GetMeQuery and the permission checker so the "which
/// permissions does this user currently hold" join lives in exactly one place.</summary>
internal static class UserPermissionLookup
{
    public static async Task<List<string>> GetPermissionCodesAsync(
        IApplicationDbContext db, Guid userId, CancellationToken cancellationToken)
    {
        return await (
            from userRole in db.UserRoles
            join role in db.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId && role.IsActive
            join rolePermission in db.RolePermissions on role.Id equals rolePermission.RoleId
            join permission in db.Permissions on rolePermission.PermissionId equals permission.Id
            select permission.Module + "." + permission.Action)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
