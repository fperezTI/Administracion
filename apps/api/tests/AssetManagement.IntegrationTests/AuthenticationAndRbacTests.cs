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
public class AuthenticationAndRbacTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
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

    private sealed record MeResponseDto(
        Guid UserId, string DisplayName, string Email, IReadOnlyCollection<string> PermissionCodes, Guid? ActiveCompanyId);
}
