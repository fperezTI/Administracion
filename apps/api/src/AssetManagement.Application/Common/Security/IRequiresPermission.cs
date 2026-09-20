namespace AssetManagement.Application.Common.Security;

/// <summary>
/// A command or query implements this to declare the single permission code required to execute it.
/// Enforced by AuthorizationBehavior for every request that implements it — see
/// docs/security/authorization-rbac.md. Requests that don't implement it run unauthenticated-safe
/// (e.g. GetMeQuery, which only ever returns the caller's own data).
/// </summary>
public interface IRequiresPermission
{
    public string PermissionCode { get; }
}
