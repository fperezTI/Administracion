namespace AssetManagement.Api.Middleware;

/// <summary>
/// Baseline security response headers (pedido: "hardening", F12) — a pure JSON API doesn't need a
/// Content-Security-Policy of its own (the frontend, which actually renders HTML, sets its own via
/// next.config.ts), but every response should still tell browsers not to guess content types or let this
/// API be framed. See docs/security/hardening.md for what's deliberately out of scope for V1 (a real edge
/// WAF, managed DDoS protection — both F13, real Azure infrastructure).
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            return Task.CompletedTask;
        });

        await next(context);
    }
}
