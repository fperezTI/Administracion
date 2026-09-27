using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.Infrastructure.Security;

/// <summary>Every real caller here is the current authenticated user (AuthorizationBehavior and
/// GlobalSearchQuery both pass `currentUser.UserId`) — CurrentUserProvisioningMiddleware already
/// resolved that user's full permission set once for this request, so this just checks it in
/// memory instead of re-running the same join per permission, per command.</summary>
public sealed class EfPermissionChecker(ICurrentUserContext currentUser) : IPermissionChecker
{
    public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken) =>
        Task.FromResult(currentUser.UserId == userId && currentUser.PermissionCodes.Contains(permissionCode));
}
