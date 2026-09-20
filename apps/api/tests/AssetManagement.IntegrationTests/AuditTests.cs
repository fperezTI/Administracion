using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Audit;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Organization;
using AssetManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>F8's audit trail — <c>AuditBehavior</c> writes an <c>AuditEntry</c> for every
/// <c>IAuditableCommand</c>, success or failure, queryable via <c>Audit.Read</c>.</summary>
public class AuditTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task A_successful_auditable_command_is_recorded_with_its_details()
    {
        var (client, companyId) = await ArrangeUserAsync("Assets.Create", "Assets.Read", "Catalogs.Read", "Audit.Read");

        Guid categoryId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            categoryId = await db.AssetCategories.Select(c => c.Id).FirstAsync();
        }

        var createResponse = await client.PostAsJsonAsync("/api/v1/assets", new
        {
            companyId,
            assetCategoryId = categoryId,
            brand = "Dell",
            model = "Latitude 5450",
            physicalCondition = "Good",
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var entries = await client.GetFromJsonAsync<PagedResultDto<AuditEntrySummary>>(
            $"/api/v1/audit-entries?companyId={companyId}", JsonOptions);
        var entry = entries!.Items.Should().ContainSingle(e => e.CommandName == "CreateAssetCommand").Subject;
        entry.Succeeded.Should().BeTrue();
        entry.Module.Should().Be("Assets");
        entry.Action.Should().Be("Create");
        entry.DetailsJson.Should().Contain("Dell");
    }

    [Fact]
    public async Task A_failed_auditable_command_is_recorded_with_the_error()
    {
        var (client, _) = await ArrangeUserAsync("Assets.Decommission", "Audit.Read");
        var missingAssetId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync($"/api/v1/assets/{missingAssetId}/decommission", new { justification = "No se usa" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // RequestAssetDecommissionCommand has no CompanyId property of its own (only AssetId), so
        // AuditBehavior's best-effort reflection records CompanyId = null for it — Audit.Read is a
        // tenant-wide permission (no company filter applied here), so the entry is still found.
        var entries = await client.GetFromJsonAsync<PagedResultDto<AuditEntrySummary>>("/api/v1/audit-entries", JsonOptions);
        var failedEntry = entries!.Items.Should().Contain(e => e.CommandName == "RequestAssetDecommissionCommand" && !e.Succeeded
            && e.ErrorMessage != null && e.ErrorMessage.Contains(missingAssetId.ToString())).Subject;
        failedEntry.CompanyId.Should().BeNull();
    }

    private async Task<(HttpClient Client, Guid CompanyId)> ArrangeUserAsync(params string[] permissionCodes)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Audit Tester");
        client.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);

            var company = Company.Create(
                $"Auditoria {entraObjectId} S.A.", "Empresa Auditoría", $"TAX-AUD-{entraObjectId}", "MXN",
                "America/Mexico_City", DateTimeOffset.UtcNow);
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            companyId = company.Id;

            user.GrantCompanyAccess(companyId, DateTimeOffset.UtcNow);

            var permissionIds = await db.Permissions
                .Where(p => permissionCodes.Contains(p.Module + "." + p.Action))
                .Select(p => p.Id)
                .ToListAsync();
            var role = Role.Create($"Rol {entraObjectId}", null, DateTimeOffset.UtcNow);
            role.SetPermissions(permissionIds);
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            user.AssignRole(role.Id, null, DateTimeOffset.UtcNow);

            await db.SaveChangesAsync();
        }

        return (client, companyId);
    }

    private sealed record PagedResultDto<T>(IReadOnlyCollection<T> Items, int TotalCount, int PageNumber, int PageSize);
}
