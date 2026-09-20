using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AssetManagement.Api.Middleware;
using AssetManagement.Application;
using AssetManagement.Infrastructure;
using AssetManagement.Infrastructure.Security;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Identity.Web;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId());

// Enums serialize as their string name (e.g. "Excellent", not 0) — readable API payloads, and stable
// across future re-ordering of an enum's members.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Default", policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    }
}));

// Microsoft Entra ID is the only identity provider (pedido §5). "EntraId" config (Instance, TenantId,
// ClientId/Audience) is a placeholder until real app-registration values are supplied — see
// docs/security/authentication.md and docs/architecture/00-analysis.md §18. Requests without a valid
// token simply get 401 from this handler; no code path here accepts an unauthenticated caller as
// identified.
builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("EntraId"));
builder.Services.AddAuthorization();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Basic abuse mitigation (pedido: "hardening", F12) — a real edge WAF/DDoS protection is Azure
// infrastructure (F13), not something this app can do for itself. The limit is deliberately generous
// (config-overridable — ApiWebApplicationFactory raises it further for integration tests, which already
// burst far more requests than any real user session) so it never interferes with normal UI usage, only
// with a broken or abusive client. Partitioned by the authenticated user's Entra object id when present,
// falling back to the caller's IP for unauthenticated requests.
var rateLimitPermitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 300);
var rateLimitWindowSeconds = builder.Configuration.GetValue("RateLimiting:WindowSeconds", 10);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // The fixed-window limiter doesn't set Retry-After on its own — added explicitly so a well-behaved
    // client (or a human staring at DevTools) knows exactly when to try again instead of guessing.
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = rateLimitWindowSeconds.ToString();
        return ValueTask.CompletedTask;
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var key = httpContext.User.FindFirst("oid")?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rateLimitPermitLimit,
            Window = TimeSpan.FromSeconds(rateLimitWindowSeconds),
            QueueLimit = 0,
        });
    });
});

builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("AssetManagementDb")
            ?? throw new InvalidOperationException("Missing connection string 'AssetManagementDb'."),
        name: "sql-server",
        tags: ["ready"]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();
if (!app.Environment.IsDevelopment())
{
    // HSTS: browsers that have seen this header will refuse to downgrade to plain HTTP for the
    // configured lifetime, even if a link/redirect tries to. Skipped in Development, matching
    // UseHttpsRedirection's own use throughout local Docker Compose without a trusted local cert.
    app.UseHsts();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("Default");

app.UseAuthentication();
// After authentication, so the rate limiter's partition key can read the authenticated user's claims
// (falls back to IP only for anonymous requests) — see the partitioning comment above.
app.UseRateLimiter();
// Must run after authentication (needs the validated claims) and before authorization/controllers
// (they read ICurrentUserContext / ICurrentCompanyContext, which this populates) — see
// docs/security/authorization-rbac.md.
app.UseMiddleware<CurrentUserProvisioningMiddleware>();
app.UseAuthorization();

app.MapControllers();
// Liveness: is the process itself running? No dependency checks — a down database must not make
// the orchestrator kill and restart an otherwise-healthy instance.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
// Readiness: can this instance actually serve traffic? Runs dependency checks (SQL Server, ...).
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

/// <summary>Exposed for WebApplicationFactory-based integration tests.</summary>
public partial class Program;
