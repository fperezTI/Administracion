using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Identity;

/// <summary>
/// A named bundle of permissions. Permissions are global — never scoped to a company (pedido §6).
/// Roles/permissions with assignment history are never physically deleted, only deactivated.
/// </summary>
public sealed class Role : AggregateRoot<Guid>
{
    private readonly List<RolePermission> _rolePermissions = [];

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Role()
    {
    }

    private Role(Guid id, string name, string? description, DateTimeOffset nowUtc)
        : base(id)
    {
        Name = name;
        Description = description;
        CreatedAtUtc = nowUtc;
        IsActive = true;
    }

    public static Role Create(string name, string? description, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("El nombre del rol es obligatorio.");
        }

        return new Role(Guid.NewGuid(), name.Trim(), description?.Trim(), nowUtc);
    }

    /// <summary>Creates a new role that starts with a copy of this role's current permission set.</summary>
    public Role Duplicate(string newName, DateTimeOffset nowUtc)
    {
        var copy = Create(newName, Description, nowUtc);
        foreach (var permissionId in _rolePermissions.Select(rp => rp.PermissionId))
        {
            copy._rolePermissions.Add(RolePermission.Create(copy.Id, permissionId));
        }

        return copy;
    }

    public void Rename(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("El nombre del rol es obligatorio.");
        }

        Name = name.Trim();
        Description = description?.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>Replaces this role's permission matrix in one operation.</summary>
    public void SetPermissions(IEnumerable<Guid> permissionIds)
    {
        _rolePermissions.Clear();
        foreach (var permissionId in permissionIds.Distinct())
        {
            _rolePermissions.Add(RolePermission.Create(Id, permissionId));
        }
    }
}
