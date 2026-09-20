using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Identity;
using AssetManagement.Domain.Approvals;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Organization;
using AssetManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>
/// F4's core E2E flow — request decommission, approve (or reject), and confirm the asset actually moves —
/// over the real HTTP pipeline. This is the first place the domain-event dispatch added to
/// AppDbContext.SaveChangesAsync is exercised for real: unit tests use a no-op IPublisher (see
/// TestSupport/FakePublisher.cs) and never invoke AssetApprovalReactionHandler.
/// </summary>
public class ApprovalsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Approving_a_decommission_request_decommissions_the_asset()
    {
        var (adminClient, companyId, managerRoleId) = await ArrangeCompanyWithApprovalFlowAsync();
        var (approverClient, approverUserId) = await ArrangeUserAsync(companyId, managerRoleId);
        var assetId = await CreateAssetAsync(adminClient, companyId);

        var requestResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/assets/{assetId}/decommission", new { justification = "Equipo obsoleto" });
        requestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approvalInstanceId = await requestResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        (await adminClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions))!
            .Status.Should().Be(AssetStatus.PendingDecommission);

        var mine = await approverClient.GetFromJsonAsync<List<JsonElement>>("/api/v1/approvals/mine", JsonOptions);
        mine!.Should().ContainSingle(a => a.GetProperty("id").GetGuid() == approvalInstanceId);

        var approveResponse = await approverClient.PostAsJsonAsync(
            $"/api/v1/approvals/{approvalInstanceId}/approve",
            new { comment = (string?)null, signatureMechanism = "TypedConfirmation", typedFullName = "Approving Manager", signatureImageDataUrl = (string?)null });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await adminClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions))!
            .Status.Should().Be(AssetStatus.Decommissioned);
    }

    [Fact]
    public async Task Rejecting_a_decommission_request_returns_the_asset_to_InWarehouse()
    {
        var (adminClient, companyId, managerRoleId) = await ArrangeCompanyWithApprovalFlowAsync();
        var (approverClient, _) = await ArrangeUserAsync(companyId, managerRoleId);
        var assetId = await CreateAssetAsync(adminClient, companyId);

        var requestResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/assets/{assetId}/decommission", new { justification = "Equipo obsoleto" });
        var approvalInstanceId = await requestResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var rejectResponse = await approverClient.PostAsJsonAsync(
            $"/api/v1/approvals/{approvalInstanceId}/reject",
            new { comment = "No procede", signatureMechanism = "TypedConfirmation", typedFullName = "Approving Manager", signatureImageDataUrl = (string?)null });
        rejectResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await adminClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions))!
            .Status.Should().Be(AssetStatus.InWarehouse);
    }

    [Fact]
    public async Task The_requester_cannot_approve_their_own_request()
    {
        var (adminClient, companyId, managerRoleId) = await ArrangeCompanyWithApprovalFlowAsync(requesterAlsoHoldsApproverRole: true);
        var assetId = await CreateAssetAsync(adminClient, companyId);

        var requestResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/assets/{assetId}/decommission", new { justification = "Equipo obsoleto" });
        var approvalInstanceId = await requestResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var approveResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/approvals/{approvalInstanceId}/approve",
            new { comment = (string?)null, signatureMechanism = "TypedConfirmation", typedFullName = "Self", signatureImageDataUrl = (string?)null });

        approveResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private async Task<Guid> CreateAssetAsync(HttpClient client, Guid companyId)
    {
        Guid categoryId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            categoryId = await db.AssetCategories.Select(c => c.Id).FirstAsync();
        }

        var response = await client.PostAsJsonAsync("/api/v1/assets", new
        {
            companyId,
            assetCategoryId = categoryId,
            brand = "Dell",
            model = "Latitude 5450",
            physicalCondition = "Good",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateAssetResult>(JsonOptions);
        return created!.AssetId;
    }

    private async Task<(HttpClient Client, Guid UserId)> ArrangeUserAsync(Guid companyId, Guid roleId)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Approving Manager");
        client.DefaultRequestHeaders.Add("Test-Email", "manager@example.com");

        var me = await client.GetFromJsonAsync<MeResponse>("/api/v1/me", JsonOptions);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Id == me!.UserId);
        user.GrantCompanyAccess(companyId, DateTimeOffset.UtcNow);
        user.AssignRole(roleId, null, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();

        return (client, me!.UserId);
    }

    /// <summary>Sets up a company, an admin client with decommission + approvals-configure permissions,
    /// and an active "asset.decommission" flow requiring one approval from a freshly-created "Manager"
    /// role. When <paramref name="requesterAlsoHoldsApproverRole"/> is true the admin is granted the
    /// Manager role too, to exercise the anti-self-approval rule.</summary>
    private async Task<(HttpClient Client, Guid CompanyId, Guid ManagerRoleId)> ArrangeCompanyWithApprovalFlowAsync(
        bool requesterAlsoHoldsApproverRole = false)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Decommission Requester");
        client.DefaultRequestHeaders.Add("Test-Email", "requester@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyId;
        Guid managerRoleId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);

            var company = Company.Create(
                $"Empresa {entraObjectId} S.A.", "Empresa Prueba", $"TAX-{entraObjectId}", "MXN",
                "America/Mexico_City", DateTimeOffset.UtcNow);
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            companyId = company.Id;

            user.GrantCompanyAccess(companyId, DateTimeOffset.UtcNow);

            var requesterPermissionCodes = new[] { "Assets.Create", "Assets.Read", "Catalogs.Read", "Assets.Decommission", "Approvals.Configure", "Approvals.Read" };
            var permissionIds = await db.Permissions
                .Where(p => requesterPermissionCodes.Contains(p.Module + "." + p.Action))
                .Select(p => p.Id)
                .ToListAsync();

            var requesterRole = Role.Create($"Solicitante {entraObjectId}", null, DateTimeOffset.UtcNow);
            requesterRole.SetPermissions(permissionIds);
            db.Roles.Add(requesterRole);

            var managerRole = Role.Create($"Manager {entraObjectId}", null, DateTimeOffset.UtcNow);
            db.Roles.Add(managerRole);
            await db.SaveChangesAsync();
            managerRoleId = managerRole.Id;

            user.AssignRole(requesterRole.Id, null, DateTimeOffset.UtcNow);
            if (requesterAlsoHoldsApproverRole)
            {
                user.AssignRole(managerRoleId, null, DateTimeOffset.UtcNow);
            }
            await db.SaveChangesAsync();

            // Scoped to this test's own company (not the global CompanyId == null fallback) — these
            // integration tests share one database via IClassFixture, so a global flow from one test
            // could otherwise be picked up by another test's unrelated company/role.
            var flow = ApprovalFlowDefinition.Create(
                "asset.decommission", companyId, [managerRoleId], 1, ApprovalMode.Parallel, false, DateTimeOffset.UtcNow, null);
            db.ApprovalFlowDefinitions.Add(flow);
            await db.SaveChangesAsync();
        }

        return (client, companyId, managerRoleId);
    }
}
