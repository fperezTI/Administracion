using System.Security.Claims;
using AssetManagement.Application.Identity;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace AssetManagement.Infrastructure.Security;

/// <summary>
/// Runs once per request, right after authentication: ensures the local User profile exists
/// (pedido §5, first-login provisioning), records the login, populates the per-request identity and
/// active-company state that HttpContextCurrentUserContext / HttpContextCurrentCompanyContext read
/// synchronously, and rejects requests from a deactivated account. Anonymous requests pass through
/// untouched — endpoints that need a caller enforce that via [Authorize] / AuthorizationBehavior.
/// </summary>
public sealed class CurrentUserProvisioningMiddleware(RequestDelegate next)
{
    private const string ActiveCompanyHeader = "X-Active-Company-Id";

    // Both the v1.0 short claim and the v2.0 long-form URI are checked because whether the JWT
    // handler remaps claim types depends on handler configuration, not something this middleware
    // should have to assume — see docs/security/authentication.md.
    private const string ObjectIdClaimLong = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    private const string ObjectIdClaimShort = "oid";

    public async Task InvokeAsync(HttpContext context, ISender mediator)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var entraObjectId = GetGuidClaim(context.User, ObjectIdClaimLong) ?? GetGuidClaim(context.User, ObjectIdClaimShort);

            if (entraObjectId is { } oid)
            {
                var displayName = context.User.FindFirst("name")?.Value ?? context.User.Identity.Name ?? "Unknown";
                var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                    ?? context.User.FindFirst("preferred_username")?.Value
                    ?? context.User.FindFirst("email")?.Value
                    ?? string.Empty;

                var snapshot = await mediator.Send(new ProvisionOrUpdateUserCommand(oid, displayName, email));

                if (!snapshot.IsActive)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("La cuenta de usuario está desactivada.");
                    return;
                }

                context.Items[RequestContextKeys.LocalUserId] = snapshot.UserId;
                context.Items[RequestContextKeys.EntraObjectId] = snapshot.EntraObjectId;
                context.Items[RequestContextKeys.DisplayName] = snapshot.DisplayName;
                context.Items[RequestContextKeys.Email] = snapshot.Email;
                context.Items[RequestContextKeys.AccessibleCompanyIds] = (IReadOnlyCollection<Guid>)snapshot.CompanyIds;

                var requestedCompany = context.Request.Headers[ActiveCompanyHeader].FirstOrDefault();
                if (Guid.TryParse(requestedCompany, out var requestedCompanyId)
                    && snapshot.CompanyIds.Contains(requestedCompanyId))
                {
                    context.Items[RequestContextKeys.ActiveCompanyId] = requestedCompanyId;
                }
            }
        }

        await next(context);
    }

    private static Guid? GetGuidClaim(ClaimsPrincipal user, string claimType) =>
        Guid.TryParse(user.FindFirst(claimType)?.Value, out var value) ? value : null;
}
