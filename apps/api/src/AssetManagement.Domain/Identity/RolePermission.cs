namespace AssetManagement.Domain.Identity;

/// <summary>Join record: a role grants a permission.</summary>
public sealed class RolePermission
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    private RolePermission()
    {
    }

    internal static RolePermission Create(Guid roleId, Guid permissionId) =>
        new() { RoleId = roleId, PermissionId = permissionId };
}
