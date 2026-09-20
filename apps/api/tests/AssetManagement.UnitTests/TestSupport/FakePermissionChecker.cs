using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.UnitTests.TestSupport;

/// <summary>For handler tests that call <see cref="IPermissionChecker"/> imperatively (outside
/// <c>AuthorizationBehavior</c>'s declarative <c>IRequiresPermission</c> pipeline) — e.g.
/// <c>GlobalSearchQueryHandler</c>, F11. Grants every code in <see cref="GrantedPermissionCodes"/>,
/// nothing else.</summary>
internal sealed class FakePermissionChecker : IPermissionChecker
{
    public HashSet<string> GrantedPermissionCodes { get; init; } = [];

    public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken) =>
        Task.FromResult(GrantedPermissionCodes.Contains(permissionCode));
}
