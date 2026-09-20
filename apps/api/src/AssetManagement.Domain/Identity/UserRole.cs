namespace AssetManagement.Domain.Identity;

/// <summary>Join record: a user holds a role. A user may hold several roles (pedido §6).</summary>
public sealed class UserRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid? AssignedByUserId { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; }

    public Role? Role { get; private set; }

    private UserRole()
    {
    }

    internal static UserRole Create(Guid userId, Guid roleId, Guid? assignedByUserId, DateTimeOffset nowUtc) =>
        new()
        {
            UserId = userId,
            RoleId = roleId,
            AssignedByUserId = assignedByUserId,
            AssignedAtUtc = nowUtc,
        };
}
