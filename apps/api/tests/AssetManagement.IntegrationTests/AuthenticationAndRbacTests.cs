using System.Net;
using System.Net.Http.Json;
using AssetManagement.Domain.Identity;
using AssetManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>
/// Exercises the real HTTP pipeline (auth -> first-login provisioning -> permission enforcement) end to
/// end. Covers two of the mandatory E2E scenarios from the pedido (§35): "validación de permisos" and
/// the authentication half of the audit trail's identity — auditing itself lands with the Audit context.
/// </summary>
public class AuthenticationAndRbacTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>, IAsyncLifetime
{
    // El primer usuario que se autentica en la vida del sistema recibe automáticamente todos los
    // permisos (bootstrap de Super Admin — ver ProvisionOrUpdateUserCommand.BootstrapSuperAdminAsync,
    // necesario porque sin cuentas locales/contraseñas alguien tiene que poder entrar a Roles/Usuarios
    // la primera vez). Las pruebas de esta clase comparten una sola base de datos vía
    // IClassFixture<ApiWebApplicationFactory>, y xUnit no garantiza el orden de ejecución de los
    // [Fact] dentro de la clase — sin este centinela, cualquier prueba que autentique un usuario nuevo
    // y espere CERO permisos podría, por azar de orden, ser ella misma ese "primer usuario" y recibir
    // todos los permisos en cambio (así falló Authenticated_request_without_the_required_permission_is_forbidden
    // de forma intermitente). InitializeAsync corre antes de cada [Fact] pero solo el primero realmente
    // crea el usuario — los siguientes son no-op porque ya existe al menos un usuario.
    private static readonly Guid BootstrapSentinelOid = Guid.Parse("00000000-0000-0000-0000-0000000000ff");

    public async Task InitializeAsync()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", BootstrapSentinelOid.ToString());
        await client.GetAsync("/api/v1/me");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Anonymous_request_to_a_protected_endpoint_is_rejected()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/companies");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticated_request_without_the_required_permission_is_forbidden()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", Guid.NewGuid().ToString());

        var response = await client.GetAsync("/api/v1/companies");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task First_authenticated_request_provisions_a_local_user_profile()
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Ada Lovelace");
        client.DefaultRequestHeaders.Add("Test-Email", "ada@example.com");

        var response = await client.GetAsync("/api/v1/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleOrDefaultAsync(u => u.EntraObjectId == entraObjectId);

        user.Should().NotBeNull();
        user!.DisplayName.Should().Be("Ada Lovelace");
        user.LastLoginAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Granting_the_required_permission_allows_the_request_to_succeed()
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Grace Hopper");
        client.DefaultRequestHeaders.Add("Test-Email", "grace@example.com");

        // First call provisions the local user; still forbidden — no role granted yet.
        (await client.GetAsync("/api/v1/companies")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);
            var readCompaniesPermissionId = await db.Permissions
                .Where(p => p.Module == "Companies" && p.Action == "Read")
                .Select(p => p.Id)
                .SingleAsync();

            var role = Role.Create("Lector de empresas", null, DateTimeOffset.UtcNow);
            role.SetPermissions([readCompaniesPermissionId]);
            db.Roles.Add(role);
            await db.SaveChangesAsync();

            user.AssignRole(role.Id, assignedByUserId: null, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/v1/companies");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_endpoint_never_requires_a_permission_and_reflects_the_callers_own_access()
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Alan Turing");
        client.DefaultRequestHeaders.Add("Test-Email", "alan@example.com");

        var response = await client.GetAsync("/api/v1/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MeResponseDto>();
        body!.DisplayName.Should().Be("Alan Turing");
        body.PermissionCodes.Should().BeEmpty();
    }

    [Fact]
    public async Task Active_company_header_is_only_honored_when_the_caller_is_actually_a_member()
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Katherine Johnson");
        client.DefaultRequestHeaders.Add("Test-Email", "katherine@example.com");

        // Provision the user, then grant membership to exactly one company.
        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();
        Guid memberCompanyId;
        var strangerCompanyId = Guid.NewGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);
            var company = Domain.Organization.Company.Create(
                "Acme Katherine S.A.", "Acme Katherine", $"TAX-{entraObjectId}", "MXN", "America/Mexico_City",
                DateTimeOffset.UtcNow);
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            memberCompanyId = company.Id;

            user.GrantCompanyAccess(memberCompanyId, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        using var memberRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        memberRequest.Headers.Add("X-Active-Company-Id", memberCompanyId.ToString());
        var memberResponse = await client.SendAsync(memberRequest);
        var memberBody = await memberResponse.Content.ReadFromJsonAsync<MeResponseDto>();

        using var strangerRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        strangerRequest.Headers.Add("X-Active-Company-Id", strangerCompanyId.ToString());
        var strangerResponse = await client.SendAsync(strangerRequest);
        var strangerBody = await strangerResponse.Content.ReadFromJsonAsync<MeResponseDto>();

        memberBody!.ActiveCompanyId.Should().Be(memberCompanyId);
        strangerBody!.ActiveCompanyId.Should().BeNull();
    }

    [Fact]
    public async Task User_without_a_preference_inherits_the_active_companys_theme_end_to_end()
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Margaret Hamilton");
        client.DefaultRequestHeaders.Add("Test-Email", "margaret@example.com");
        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);
            var company = Domain.Organization.Company.Create(
                "Acme Margaret S.A.", "Acme Margaret", $"TAX-{entraObjectId}", "MXN", "America/Mexico_City",
                DateTimeOffset.UtcNow);
            company.SetDefaultTheme("corporate-blue");
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            companyId = company.Id;
            user.GrantCompanyAccess(companyId, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/me");
        request.Headers.Add("X-Active-Company-Id", companyId.ToString());
        var body = await (await client.SendAsync(request)).Content.ReadFromJsonAsync<MeResponseDto>();

        body!.ThemePreference.Should().BeNull();
        body.EffectiveTheme.Should().Be("corporate-blue");
    }

    [Fact]
    public async Task Setting_a_personal_theme_preference_overrides_the_companys_default()
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Radia Perlman");
        client.DefaultRequestHeaders.Add("Test-Email", "radia@example.com");
        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        var putResponse = await client.PutAsJsonAsync("/api/v1/me/preferences/theme", new { themeCode = "dark" });
        putResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var body = await (await client.GetAsync("/api/v1/me")).Content.ReadFromJsonAsync<MeResponseDto>();
        body!.ThemePreference.Should().Be("dark");
        body.EffectiveTheme.Should().Be("dark");

        // Restoring inheritance (null) clears it again.
        (await client.PutAsJsonAsync("/api/v1/me/preferences/theme", new { themeCode = (string?)null }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var restoredBody = await (await client.GetAsync("/api/v1/me")).Content.ReadFromJsonAsync<MeResponseDto>();
        restoredBody!.ThemePreference.Should().BeNull();
    }

    [Fact]
    public async Task An_unknown_theme_code_is_rejected()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", Guid.NewGuid().ToString());

        var response = await client.PutAsJsonAsync("/api/v1/me/preferences/theme", new { themeCode = "neon-pink" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Only_a_caller_with_Companies_Update_can_change_a_companys_default_theme()
    {
        Guid companyId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var company = Domain.Organization.Company.Create(
                "Acme ThemeAdmin S.A.", "Acme ThemeAdmin", $"TAX-{Guid.NewGuid()}", "MXN", "America/Mexico_City",
                DateTimeOffset.UtcNow);
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            companyId = company.Id;
        }

        // No role granted yet: forbidden even though the request is otherwise well-formed.
        var strangerClient = factory.CreateClient();
        strangerClient.DefaultRequestHeaders.Add("Test-Oid", Guid.NewGuid().ToString());
        var forbidden = await strangerClient.PutAsJsonAsync(
            $"/api/v1/companies/{companyId}/preferences/theme", new { themeCode = "executive-gray" });
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Granting Companies.Update (the same permission that already gates editing a company —
        // this system has no separate "SuperAdmin" permission, see SetCompanyDefaultThemeCommand)
        // allows it.
        var adminEntraObjectId = Guid.NewGuid();
        var adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("Test-Oid", adminEntraObjectId.ToString());
        (await adminClient.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == adminEntraObjectId);
            var updateCompaniesPermissionId = await db.Permissions
                .Where(p => p.Module == "Companies" && p.Action == "Update")
                .Select(p => p.Id)
                .SingleAsync();
            var role = Role.Create("Administrador de empresas", null, DateTimeOffset.UtcNow);
            role.SetPermissions([updateCompaniesPermissionId]);
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            user.AssignRole(role.Id, assignedByUserId: null, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        var allowed = await adminClient.PutAsJsonAsync(
            $"/api/v1/companies/{companyId}/preferences/theme", new { themeCode = "executive-gray" });
        allowed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await verifyDb.Companies.SingleAsync(c => c.Id == companyId)).DefaultThemeCode.Should().Be("executive-gray");
    }

    private sealed record MeResponseDto(
        Guid UserId, string DisplayName, string Email, IReadOnlyCollection<string> PermissionCodes, Guid? ActiveCompanyId,
        string? ThemePreference, string EffectiveTheme);
}
