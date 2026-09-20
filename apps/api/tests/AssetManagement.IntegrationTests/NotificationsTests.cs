using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Identity;
using AssetManagement.Application.Notifications;
using AssetManagement.Domain.Approvals;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Organization;
using AssetManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>F8's notification center — driven by the two generic reaction handlers
/// (<c>ApprovalRequestedNotificationHandler</c>/<c>ApprovalOutcomeNotificationHandler</c>) via a real
/// internal-request approval flow (F7), proving the same hooks that also cover F4/F5's approval flows.</summary>
public class NotificationsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Submitting_a_request_notifies_the_eligible_approver()
    {
        var (requesterClient, companyId, managerRoleId) = await ArrangeCompanyAndFlowAsync();
        var (approverClient, _) = await ArrangeUserAsync(companyId, managerRoleId, "Approving Manager");
        var assetId = await CreateAssetAsync(requesterClient, companyId);

        var requestResponse = await requesterClient.PostAsJsonAsync(
            "/api/v1/requests", new { type = "AssetAssignment", assetId, justification = "Necesito equipo", expectedReturnDate = (string?)null });
        requestResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var notifications = await approverClient.GetFromJsonAsync<List<MyNotificationSummary>>("/api/v1/notifications/mine", JsonOptions);
        notifications.Should().ContainSingle(n => n.Type == "ApprovalRequested" && !n.IsRead);
    }

    [Fact]
    public async Task Approving_a_request_notifies_the_requester_of_the_outcome()
    {
        var (requesterClient, companyId, managerRoleId) = await ArrangeCompanyAndFlowAsync();
        var (approverClient, _) = await ArrangeUserAsync(companyId, managerRoleId, "Approving Manager");
        var assetId = await CreateAssetAsync(requesterClient, companyId);

        var requestResponse = await requesterClient.PostAsJsonAsync(
            "/api/v1/requests", new { type = "AssetAssignment", assetId, justification = "Necesito equipo", expectedReturnDate = (string?)null });
        var requestId = await requestResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var mine = await approverClient.GetFromJsonAsync<List<JsonElement>>("/api/v1/approvals/mine", JsonOptions);
        var approvalInstanceId = mine!.Single(a => a.GetProperty("contextId").GetGuid() == requestId).GetProperty("id").GetGuid();
        await approverClient.PostAsJsonAsync(
            $"/api/v1/approvals/{approvalInstanceId}/approve",
            new { comment = (string?)null, signatureMechanism = "TypedConfirmation", typedFullName = "Approving Manager", signatureImageDataUrl = (string?)null });

        var notifications = await requesterClient.GetFromJsonAsync<List<MyNotificationSummary>>("/api/v1/notifications/mine", JsonOptions);
        notifications.Should().ContainSingle(n => n.Type == "ApprovalCompleted");
    }

    [Fact]
    public async Task Marking_a_notification_as_read_updates_its_state()
    {
        var (requesterClient, companyId, managerRoleId) = await ArrangeCompanyAndFlowAsync();
        var (approverClient, _) = await ArrangeUserAsync(companyId, managerRoleId, "Approving Manager");
        var assetId = await CreateAssetAsync(requesterClient, companyId);

        await requesterClient.PostAsJsonAsync(
            "/api/v1/requests", new { type = "AssetAssignment", assetId, justification = "Necesito equipo", expectedReturnDate = (string?)null });

        var before = await approverClient.GetFromJsonAsync<List<MyNotificationSummary>>("/api/v1/notifications/mine", JsonOptions);
        var notificationId = before!.Single(n => n.Type == "ApprovalRequested").Id;

        var readResponse = await approverClient.PostAsync($"/api/v1/notifications/{notificationId}/read", null);
        readResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await approverClient.GetFromJsonAsync<List<MyNotificationSummary>>("/api/v1/notifications/mine", JsonOptions);
        after!.Single(n => n.Id == notificationId).IsRead.Should().BeTrue();
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

    private async Task<(HttpClient RequesterClient, Guid CompanyId, Guid ManagerRoleId)> ArrangeCompanyAndFlowAsync()
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Notification Requester");
        client.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyId;
        Guid managerRoleId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);

            var company = Company.Create(
                $"Notificaciones {entraObjectId} S.A.", "Empresa Notificaciones", $"TAX-NOTIF-{entraObjectId}", "MXN",
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

            var flow = ApprovalFlowDefinition.Create(
                "internal-request.asset-assignment", companyId, [managerRoleId], 1, ApprovalMode.Parallel, false, DateTimeOffset.UtcNow, null);
            db.ApprovalFlowDefinitions.Add(flow);
            await db.SaveChangesAsync();
        }

        return (client, companyId, managerRoleId);
    }
}
