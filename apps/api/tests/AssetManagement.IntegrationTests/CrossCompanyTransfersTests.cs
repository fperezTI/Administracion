using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Identity;
using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Approvals;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Inventory;
using AssetManagement.Domain.Organization;
using AssetManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>
/// F5's core E2E flow — request a cross-company transfer, approve it (real domain-event dispatch, same
/// pipeline F4 proved), confirm the asset reaches InTransit, receive it at the destination with a
/// signature, and confirm CompanyId/InternalFolio actually changed while AssetTag.Code did not (ADR
/// 0004/0007) — over the real HTTP pipeline against SQL Server.
/// </summary>
public class CrossCompanyTransfersTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Full_transfer_lifecycle_end_to_end()
    {
        var (sourceClient, sourceCompanyId, destinationCompanyId, managerRoleId) = await ArrangeCompaniesAndApprovalFlowAsync();
        var (approverClient, _) = await ArrangeUserAsync(sourceCompanyId, managerRoleId, "Approving Manager");
        var (receiverClient, _) = await ArrangeUserAsync(
            destinationCompanyId, null, "Destination Receiver", "Transfers.Update", "Transfers.Read", "Assets.Read", "Movements.Read");

        var assetId = await CreateAssetAsync(sourceClient, sourceCompanyId);
        var originalDetail = await sourceClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions);
        var originalTagCode = originalDetail!.Tag!.Code;

        var requestResponse = await sourceClient.PostAsJsonAsync(
            "/api/v1/transfers", new { assetId, toCompanyId = destinationCompanyId, notes = "Consolidación de equipo" });
        requestResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var transferId = await requestResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var mine = await approverClient.GetFromJsonAsync<List<JsonElement>>("/api/v1/approvals/mine", JsonOptions);
        var approvalInstanceId = mine!.Single(a => a.GetProperty("contextId").GetGuid() == transferId).GetProperty("id").GetGuid();

        var approveResponse = await approverClient.PostAsJsonAsync(
            $"/api/v1/approvals/{approvalInstanceId}/approve",
            new { comment = (string?)null, signatureMechanism = "TypedConfirmation", typedFullName = "Approving Manager", signatureImageDataUrl = (string?)null });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await sourceClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions))!
            .Status.Should().Be(AssetStatus.InTransit);

        var transferAfterDeparture = await sourceClient.GetFromJsonAsync<TransferDetail>($"/api/v1/transfers/{transferId}", JsonOptions);
        transferAfterDeparture!.Status.Should().Be(TransferStatus.InTransit);

        var receiveResponse = await receiverClient.PostAsJsonAsync(
            $"/api/v1/transfers/{transferId}/receive",
            new { signatureMechanism = "TypedConfirmation", typedFullName = "Destination Receiver", signatureImageDataUrl = (string?)null });
        receiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var received = await receiverClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions);
        received!.Status.Should().Be(AssetStatus.InWarehouse);
        received.CompanyId.Should().Be(destinationCompanyId);
        received.InternalFolio.Should().MatchRegex(@"^ASSET-\d{6}$");
        received.Tag!.Code.Should().Be(originalTagCode, "AssetTag.Code must survive a transfer unchanged (ADR 0004/0007)");

        var transferAfterReceipt = await receiverClient.GetFromJsonAsync<TransferDetail>($"/api/v1/transfers/{transferId}", JsonOptions);
        transferAfterReceipt!.Status.Should().Be(TransferStatus.Completed);

        var destinationMovements = await receiverClient.GetFromJsonAsync<PagedResultDto<MovementSummary>>(
            $"/api/v1/movements?companyId={destinationCompanyId}&assetId={assetId}", JsonOptions);
        destinationMovements!.Items.Should().ContainSingle(m => m.Type == MovementType.CrossCompanyTransferIn && m.Status == MovementStatus.Completed);
    }

    [Fact]
    public async Task Rejecting_a_transfer_leaves_the_asset_in_the_source_company()
    {
        var (sourceClient, sourceCompanyId, destinationCompanyId, managerRoleId) = await ArrangeCompaniesAndApprovalFlowAsync();
        var (approverClient, _) = await ArrangeUserAsync(sourceCompanyId, managerRoleId, "Approving Manager");

        var assetId = await CreateAssetAsync(sourceClient, sourceCompanyId);

        var requestResponse = await sourceClient.PostAsJsonAsync(
            "/api/v1/transfers", new { assetId, toCompanyId = destinationCompanyId, notes = (string?)null });
        var transferId = await requestResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var mine = await approverClient.GetFromJsonAsync<List<JsonElement>>("/api/v1/approvals/mine", JsonOptions);
        var approvalInstanceId = mine!.Single(a => a.GetProperty("contextId").GetGuid() == transferId).GetProperty("id").GetGuid();

        var rejectResponse = await approverClient.PostAsJsonAsync(
            $"/api/v1/approvals/{approvalInstanceId}/reject",
            new { comment = "No procede", signatureMechanism = "TypedConfirmation", typedFullName = "Approving Manager", signatureImageDataUrl = (string?)null });
        rejectResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await sourceClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions))!
            .Status.Should().Be(AssetStatus.InWarehouse);

        var transfer = await sourceClient.GetFromJsonAsync<TransferDetail>($"/api/v1/transfers/{transferId}", JsonOptions);
        transfer!.Status.Should().Be(TransferStatus.Rejected);
    }

    [Fact]
    public async Task Receiving_without_access_to_the_destination_company_is_forbidden()
    {
        var (sourceClient, sourceCompanyId, destinationCompanyId, managerRoleId) = await ArrangeCompaniesAndApprovalFlowAsync();
        var (approverClient, _) = await ArrangeUserAsync(sourceCompanyId, managerRoleId, "Approving Manager");

        var assetId = await CreateAssetAsync(sourceClient, sourceCompanyId);
        var requestResponse = await sourceClient.PostAsJsonAsync(
            "/api/v1/transfers", new { assetId, toCompanyId = destinationCompanyId, notes = (string?)null });
        var transferId = await requestResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var mine = await approverClient.GetFromJsonAsync<List<JsonElement>>("/api/v1/approvals/mine", JsonOptions);
        var approvalInstanceId = mine!.Single(a => a.GetProperty("contextId").GetGuid() == transferId).GetProperty("id").GetGuid();
        await approverClient.PostAsJsonAsync(
            $"/api/v1/approvals/{approvalInstanceId}/approve",
            new { comment = (string?)null, signatureMechanism = "TypedConfirmation", typedFullName = "Approving Manager", signatureImageDataUrl = (string?)null });

        // sourceClient has Transfers.Create/Read but no access to the destination company at all.
        var receiveResponse = await sourceClient.PostAsJsonAsync(
            $"/api/v1/transfers/{transferId}/receive",
            new { signatureMechanism = "TypedConfirmation", typedFullName = "Someone Unauthorized", signatureImageDataUrl = (string?)null });

        receiveResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

    private async Task<(HttpClient Client, Guid UserId)> ArrangeUserAsync(
        Guid companyId, Guid? roleId, string displayName, params string[] additionalPermissionCodes)
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

        if (additionalPermissionCodes.Length > 0)
        {
            var permissionIds = await db.Permissions
                .Where(p => additionalPermissionCodes.Contains(p.Module + "." + p.Action))
                .Select(p => p.Id)
                .ToListAsync();
            var role = Role.Create($"Rol {entraObjectId}", null, DateTimeOffset.UtcNow);
            role.SetPermissions(permissionIds);
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            user.AssignRole(role.Id, null, DateTimeOffset.UtcNow);
        }

        await db.SaveChangesAsync();

        return (client, me!.UserId);
    }

    /// <summary>Two companies, a requester client with Transfers.Create + Assets.Create/Read +
    /// Catalogs.Read + Approvals.Read on the source company, a "Manager" role, and an active
    /// "asset.cross-company-transfer" approval flow scoped to the source company (own CompanyId, not the
    /// global null fallback — same isolation fix ApprovalsTests needed).</summary>
    private async Task<(HttpClient SourceClient, Guid SourceCompanyId, Guid DestinationCompanyId, Guid ManagerRoleId)>
        ArrangeCompaniesAndApprovalFlowAsync()
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Transfer Requester");
        client.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid sourceCompanyId;
        Guid destinationCompanyId;
        Guid managerRoleId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);

            var sourceCompany = Company.Create(
                $"Origen {entraObjectId} S.A.", "Empresa Origen", $"TAX-SRC-{entraObjectId}", "MXN",
                "America/Mexico_City", DateTimeOffset.UtcNow);
            var destinationCompany = Company.Create(
                $"Destino {entraObjectId} S.A.", "Empresa Destino", $"TAX-DST-{entraObjectId}", "MXN",
                "America/Mexico_City", DateTimeOffset.UtcNow);
            db.Companies.AddRange(sourceCompany, destinationCompany);
            await db.SaveChangesAsync();
            sourceCompanyId = sourceCompany.Id;
            destinationCompanyId = destinationCompany.Id;

            user.GrantCompanyAccess(sourceCompanyId, DateTimeOffset.UtcNow);

            var requesterPermissionCodes = new[] { "Assets.Create", "Assets.Read", "Catalogs.Read", "Transfers.Create", "Transfers.Read", "Approvals.Read" };
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
                "asset.cross-company-transfer", sourceCompanyId, [managerRoleId], 1, ApprovalMode.Parallel, false,
                DateTimeOffset.UtcNow, null);
            db.ApprovalFlowDefinitions.Add(flow);
            await db.SaveChangesAsync();
        }

        return (client, sourceCompanyId, destinationCompanyId, managerRoleId);
    }

    private sealed record PagedResultDto<T>(IReadOnlyCollection<T> Items, int TotalCount, int PageNumber, int PageSize);
}
