using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AssetManagement.IntegrationTests;

/// <summary>
/// Replaces the real Entra ID JwtBearer handler in tests: a request carrying the "Test-Oid" header is
/// treated as authenticated with that Entra object id, exercising the exact same claims-reading code in
/// CurrentUserProvisioningMiddleware as production — only how the token is validated differs. No test
/// ever needs (or has) a real Entra ID app registration.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Test-Oid", out var oidValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new("oid", oidValues.ToString()),
            new("name", Request.Headers["Test-Name"].FirstOrDefault() ?? "Test User"),
            new(ClaimTypes.Email, Request.Headers["Test-Email"].FirstOrDefault() ?? "test@example.com"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
