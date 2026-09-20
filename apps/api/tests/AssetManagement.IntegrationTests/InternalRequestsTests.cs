using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Identity;
using AssetManagement.Application.Requests;
using AssetManagement.Domain.Approvals;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Organization;
using AssetManagement.Domain.Requests;
using AssetManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>
/// F7's core flow — submit an internal request, approve it (real domain-event dispatch, same pipeline
/// F4/F5/F6 proved), and confirm the right downstream record (Assignment/Loan/MaintenanceOrder) was
/// created automatically and the asset moved to the matching status — over the real HTTP pipeline against
/// SQL Server.
/// </summary>
public class InternalRequestsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task AssetAssignment_request_approved_creates_an_assignment_and_reserves_the_asset()
    {
        var (requesterClient, companyId, managerRoleId) = await ArrangeCompanyAndFlowsAsync();
        var (approverClient, _) = await ArrangeUserAsync(companyId, managerRoleId, "Approving Manager");
        var assetId = await CreateAssetAsync(requesterClient, companyId);

        var requestResponse = await requesterClient.PostAsJsonAsync(
            "/api/v1/requests", new { type = "AssetAssignment", assetId, justification = "Necesito equipo para trabajar", expectedReturnDate = (string?)null });
        requestResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var requestId = await requestResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        await ApproveAsync(approverClient, requestId, "Approving Manager");

        var asset = await requesterClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions);
        asset!.Status.Should().Be(AssetStatus.Reserved);

        var detail = await requesterClient.GetFromJsonAsync<InternalRequestDetail>($"/api/v1/requests/{requestId}", JsonOptions);
        detail!.Status.Should().Be(InternalRequestStatus.Fulfilled);
        detail.FulfillmentReferenceId.Should().NotBeNull();
    }

    [Fact]
    public async Task Loan_request_approved_creates_a_loan_and_puts_the_asset_on_loan()
    {
        var (requesterClient, companyId, managerRoleId) = await ArrangeCompanyAndFlowsAsync();
        var (approverClient, _) = await ArrangeUserAsync(companyId, managerRoleId, "Approving Manager");
        var assetId = await CreateAssetAsync(requesterClient, companyId);

        var requestResponse = await requesterClient.PostAsJsonAsync(
            "/api/v1/requests", new { type = "Loan", assetId, justification = "Préstamo para viaje", expectedReturnDate = "2026-10-15" });
        var requestId = await requestResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        await ApproveAsync(approverClient, requestId, "Approving Manager");

        var asset = await requesterClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions);
        asset!.Status.Should().Be(AssetStatus.OnLoan);
    }

    [Fact]
    public async Task Maintenance_request_approved_opens_a_maintenance_order_and_the_asset_goes_InMaintenance()
    {
        var (requesterClient, companyId, managerRoleId) = await ArrangeCompanyAndFlowsAsync();
        var (approverClient, _) = await ArrangeUserAsync(companyId, managerRoleId, "Approving Manager");
        var assetId = await CreateAssetAsync(requesterClient, companyId);

        var requestResponse = await requesterClient.PostAsJsonAsync(
            "/api/v1/requests", new { type = "Maintenance", assetId, justification = "No enciende", expectedReturnDate = (string?)null });
        var requestId = await requestResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        await ApproveAsync(approverClient, requestId, "Approving Manager");

        var asset = await requesterClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions);
        asset!.Status.Should().Be(AssetStatus.InMaintenance);
    }

    [Fact]
    public async Task Rejecting_a_request_leaves_the_asset_untouched()
    {
        var (requesterClient, companyId, managerRoleId) = await ArrangeCompanyAndFlowsAsync();
        var (approverClient, _) = await ArrangeUserAsync(companyId, managerRoleId, "Approving Manager");
        var assetId = await CreateAssetAsync(requesterClient, companyId);

        var requestResponse = await requesterClient.PostAsJsonAsync(
            "/api/v1/requests", new { type = "AssetAssignment", assetId, justification = "Necesito equipo", expectedReturnDate = (string?)null });
        var requestId = await requestResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var mine = await approverClient.GetFromJsonAsync<List<JsonElement>>("/api/v1/approvals/mine", JsonOptions);
        var approvalInstanceId = mine!.Single(a => a.GetProperty("contextId").GetGuid() == requestId).GetProperty("id").GetGuid();
        var rejectResponse = await approverClient.PostAsJsonAsync(
            $"/api/v1/approvals/{approvalInstanceId}/reject",
            new { comment = "No procede", signatureMechanism = "TypedConfirmation", typedFullName = "Approving Manager", signatureImageDataUrl = (string?)null });
        rejectResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var asset = await requesterClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions);
        asset!.Status.Should().Be(AssetStatus.InWarehouse);

        var detail = await requesterClient.GetFromJsonAsync<InternalRequestDetail>($"/api/v1/requests/{requestId}", JsonOptions);
        detail!.Status.Should().Be(InternalRequestStatus.Rejected);
    }

    [Fact]
    public async Task The_requester_can_cancel_their_own_pending_request()
    {
        var (requesterClient, companyId, _) = await ArrangeCompanyAndFlowsAsync();
        var assetId = await CreateAssetAsync(requesterClient, companyId);

        var requestResponse = await requesterClient.PostAsJsonAsync(
            "/api/v1/requests", new { type = "AssetAssignment", assetId, justification = "Necesito equipo", expectedReturnDate = (string?)null });
        var requestId = await requestResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var cancelResponse = await requesterClient.PostAsync($"/api/v1/requests/{requestId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var mine = await requesterClient.GetFromJsonAsync<List<MyInternalRequestSummary>>("/api/v1/requests/mine", JsonOptions);
        mine!.Single(r => r.Id == requestId).Status.Should().Be(InternalRequestStatus.Cancelled);
    }

    private static async Task ApproveAsync(HttpClient approverClient, Guid requestId, string approverName)
    {
        var mine = await approverClient.GetFromJsonAsync<List<JsonElement>>("/api/v1/approvals/mine", JsonOptions);
        var approvalInstanceId = mine!.Single(a => a.GetProperty("contextId").GetGuid() == requestId).GetProperty("id").GetGuid();

        var approveResponse = await approverClient.PostAsJsonAsync(
            $"/api/v1/approvals/{approvalInstanceId}/approve",
            new { comment = (string?)null, signatureMechanism = "TypedConfirmation", typedFullName = approverName, signatureImageDataUrl = (string?)null });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
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

    private async Task<(HttpClient Client, Guid UserId)> ArrangeUserAsync(Guid companyId, Guid? roleId, string displayName)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", displayName);
        client.DefaultRequestHeaders.Add("Test-Email", $"{Guid.NewGuid()}@example.com");

        var me = await client.GetFromJsonAsync<MeResponse>("/api/v1/me", JsonOptions);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Id == me!.UserId);
        user.GrantCompanyAccess(companyId, DateTimeOffset.UtcNow);

        if (roleId is { } rid)
        {
            user.AssignRole(rid, null, DateTimeOffset.UtcNow);
        }

        await db.SaveChangesAsync();

        return (client, me!.UserId);
    }

    /// <summary>A company, a requester client with Assets.Create/Read + Catalogs.Read + Requests.Create/Read
    /// + Approvals.Read, a "Manager" role, and active flows for all three internal-request types, all
    /// scoped to this test's own company (not the global null fallback — same isolation fix earlier
    /// integration tests needed).</summary>
    private async Task<(HttpClient RequesterClient, Guid CompanyId, Guid ManagerRoleId)> ArrangeCompanyAndFlowsAsync()
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Request Requester");
        client.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyId;
        Guid managerRoleId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);

            var company = Company.Create(
                $"Solicitudes {entraObjectId} S.A.", "Empresa Solicitudes", $"TAX-REQ-{entraObjectId}", "MXN",
                "America/Mexico_City", DateTimeOffset.UtcNow);
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            companyId = company.Id;

            user.GrantCompanyAccess(companyId, DateTimeOffset.UtcNow);

            var requesterPermissionCodes = new[] { "Assets.Create", "Assets.Read", "Catalogs.Read", "Requests.Create", "Requests.Read", "Approvals.Read" };
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
            await db.SaveChangesAsync();

            var flowKeys = new[] { "internal-request.asset-assignment", "internal-request.loan", "internal-request.maintenance" };
            foreach (var flowKey in flowKeys)
            {
                var flow = ApprovalFlowDefinition.Create(flowKey, companyId, [managerRoleId], 1, ApprovalMode.Parallel, false, DateTimeOffset.UtcNow, null);
                db.ApprovalFlowDefinitions.Add(flow);
            }
            await db.SaveChangesAsync();
        }

        return (client, companyId, managerRoleId);
    }
}
