using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using MediatR;

namespace AssetManagement.Application.Common.Behaviors;

/// <summary>
/// Defense-in-depth authorization: revalidates the permission required by a request regardless of
/// which endpoint (or future non-HTTP entry point) dispatched it — see
/// docs/security/authorization-rbac.md, layer 3. Requests that don't implement IRequiresPermission
/// pass through unchecked (they must not touch another user's data — e.g. GetMeQuery).
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse>(
    ICurrentUserContext currentUser,
    IPermissionChecker permissionChecker)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IRequiresPermission requiresPermission)
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
            {
                throw new ForbiddenAccessException(
                    $"Authentication is required to execute {typeof(TRequest).Name}.");
            }

            var hasPermission = await permissionChecker.HasPermissionAsync(
                userId, requiresPermission.PermissionCode, cancellationToken);

            if (!hasPermission)
            {
                throw new ForbiddenAccessException(
                    $"Missing permission '{requiresPermission.PermissionCode}' required by {typeof(TRequest).Name}.");
            }
        }

        return await next();
    }
}
