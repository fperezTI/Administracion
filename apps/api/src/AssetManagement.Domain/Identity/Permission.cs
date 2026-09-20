using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Identity;

/// <summary>
/// A single grantable capability, shaped "{Module}.{Action}" (pedido §6). The catalog is seeded by
/// migration, not created through the API in V1 — see docs/security/authorization-rbac.md.
/// </summary>
public sealed class Permission : Entity<Guid>
{
    public string Module { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    public string Code => $"{Module}.{Action}";

    private Permission()
    {
    }

    public Permission(Guid id, string module, string action, string description)
        : base(id)
    {
        Module = module;
        Action = action;
        Description = description;
    }
}
