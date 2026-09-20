using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Identity;
using AssetManagement.Application.Inventory;
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
/// Covers F3's core E2E flow — assign, sign (self-service, by a different authenticated user than the
/// one who created the assignment), and return — plus loan/return and relocation, over the real HTTP
/// pipeline including the real EfFolioGenerator (per-type movement folios).
/// </summary>
public class InventoryOperationsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Assign_sign_and_return_moves_the_asset_through_Reserved_Assigned_and_back_to_InWarehouse()
    {
        var (adminClient, companyId, _) = await ArrangeAuthenticatedClientAsync(
            "Assets.Create", "Assets.Read", "Catalogs.Read", "Assignments.Create", "Assignments.Read", "Returns.Create");
        var (recipientClient, recipientUserId) = await ArrangeRecipientAsync(companyId);

        var assetId = await CreateAssetAsync(adminClient, companyId);

        var createAssignment = await adminClient.PostAsJsonAsync("/api/v1/assignments", new
        {
            assetId,
            assignedToUserId = recipientUserId,
            orgUnitId = (Guid?)null,
            notes = "Asignación de prueba E2E",
        });
        createAssignment.StatusCode.Should().Be(HttpStatusCode.Created);
        var assignment = await createAssignment.Content.ReadFromJsonAsync<CreateAssignmentResult>(JsonOptions);

        (await adminClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions))!
            .Status.Should().Be(AssetStatus.Reserved);

        // Someone other than the recipient may not sign.
        var forbiddenSign = await adminClient.PostAsJsonAsync(
            $"/api/v1/assignments/{assignment!.AssignmentId}/sign", new { typedFullName = "Not The Recipient" });
        forbiddenSign.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Self-service listing — exercises the real SQL Server provider (unlike the InMemory provider
        // used by unit tests, this is what actually caught the "orderby a.Status == X" translation bug).
        var mineBeforeSigning = await recipientClient.GetFromJsonAsync<List<MyAssignmentSummary>>(
            "/api/v1/assignments/mine", JsonOptions);
        mineBeforeSigning!.Should().ContainSingle(a => a.Id == assignment.AssignmentId && a.Status == AssignmentStatus.PendingSignature);

        var sign = await recipientClient.PostAsJsonAsync(
            $"/api/v1/assignments/{assignment.AssignmentId}/sign", new { typedFullName = "Asset Recipient" });
        sign.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var mineAfterSigning = await recipientClient.GetFromJsonAsync<List<MyAssignmentSummary>>(
            "/api/v1/assignments/mine", JsonOptions);
        mineAfterSigning!.Should().ContainSingle(a => a.Id == assignment.AssignmentId && a.Status == AssignmentStatus.Accepted);

        (await adminClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions))!
            .Status.Should().Be(AssetStatus.Assigned);

        var detail = await adminClient.GetFromJsonAsync<AssignmentDetail>(
            $"/api/v1/assignments/{assignment.AssignmentId}", JsonOptions);
        detail!.Status.Should().Be(AssignmentStatus.Accepted);
        detail.AcceptanceSignature!.SignerDisplayName.Should().Be("Asset Recipient");

        var returnResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/assignments/{assignment.AssignmentId}/return", new { typedFullName = "Warehouse Clerk", notes = (string?)null });
        returnResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await adminClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions))!
            .Status.Should().Be(AssetStatus.InWarehouse);
    }

    [Fact]
    public async Task Loan_and_return_moves_the_asset_through_OnLoan_and_back_to_InWarehouse()
    {
        var (adminClient, companyId, _) = await ArrangeAuthenticatedClientAsync(
            "Assets.Create", "Assets.Read", "Catalogs.Read", "Loans.Create", "Loans.Read", "Returns.Create");
        var (_, borrowerUserId) = await ArrangeRecipientAsync(companyId);

        var assetId = await CreateAssetAsync(adminClient, companyId);

        var createLoan = await adminClient.PostAsJsonAsync("/api/v1/loans", new
        {
            assetId,
            borrowerUserId,
            expectedReturnDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            notes = (string?)null,
        });
        createLoan.StatusCode.Should().Be(HttpStatusCode.Created);
        var loan = await createLoan.Content.ReadFromJsonAsync<CreateLoanResult>(JsonOptions);

        (await adminClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions))!
            .Status.Should().Be(AssetStatus.OnLoan);

        var returnResponse = await adminClient.PostAsJsonAsync($"/api/v1/loans/{loan!.LoanId}/return", new { notes = (string?)null });
        returnResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await adminClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions))!
            .Status.Should().Be(AssetStatus.InWarehouse);
    }

    [Fact]
    public async Task Relocating_an_asset_records_a_movement_without_changing_its_status()
    {
        var (adminClient, companyId, _) = await ArrangeAuthenticatedClientAsync(
            "Assets.Create", "Assets.Read", "Catalogs.Read", "Assets.Update", "Movements.Read");
        var assetId = await CreateAssetAsync(adminClient, companyId);

        Guid orgUnitId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // "Warehouse" is one of the 11 org unit types already seeded by migration (ADR 0002) —
            // reuse it rather than inserting a colliding one (SQL Server's default collation is
            // case-insensitive, so a literal "WAREHOUSE" would violate the unique index on Code).
            var orgUnitTypeId = await db.OrgUnitTypes.Where(t => t.Code == "Warehouse").Select(t => t.Id).SingleAsync();
            var orgUnit = OrgUnit.Create(companyId, orgUnitTypeId, null, "Almacén CDMX", "ALM-CDMX", DateTimeOffset.UtcNow);
            db.OrgUnits.Add(orgUnit);
            await db.SaveChangesAsync();
            orgUnitId = orgUnit.Id;
        }

        var relocate = await adminClient.PostAsJsonAsync($"/api/v1/assets/{assetId}/relocate", new { newOrgUnitId = orgUnitId, notes = "Reubicación E2E" });
        relocate.StatusCode.Should().Be(HttpStatusCode.OK);

        var asset = await adminClient.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions);
        asset!.Status.Should().Be(AssetStatus.InWarehouse);
        asset.CurrentOrgUnitId.Should().Be(orgUnitId);

        var movements = await adminClient.GetFromJsonAsync<PagedResultDto<MovementSummary>>(
            $"/api/v1/movements?companyId={companyId}&assetId={assetId}", JsonOptions);
        movements!.Items.Should().ContainSingle(m => m.Type == MovementType.Relocation && m.Status == MovementStatus.Completed);
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

    private async Task<(HttpClient Client, Guid UserId)> ArrangeRecipientAsync(Guid companyId)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Asset Recipient");
        client.DefaultRequestHeaders.Add("Test-Email", "recipient@example.com");

        var me = await client.GetFromJsonAsync<MeResponse>("/api/v1/me", JsonOptions);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Id == me!.UserId);
        user.GrantCompanyAccess(companyId, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();

        return (client, me!.UserId);
    }

    private async Task<(HttpClient Client, Guid CompanyId, Guid CategoryId)> ArrangeAuthenticatedClientAsync(
        params string[] permissionCodes)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Inventory Admin");
        client.DefaultRequestHeaders.Add("Test-Email", "inventory-admin@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyId;
        Guid categoryId;
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

            var permissionIds = await db.Permissions
                .Where(p => permissionCodes.Contains(p.Module + "." + p.Action))
                .Select(p => p.Id)
                .ToListAsync();

            var role = Role.Create($"Rol de prueba {entraObjectId}", null, DateTimeOffset.UtcNow);
            role.SetPermissions(permissionIds);
            db.Roles.Add(role);
            await db.SaveChangesAsync();

            user.AssignRole(role.Id, null, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            categoryId = await db.AssetCategories.Select(c => c.Id).FirstAsync();
        }

        return (client, companyId, categoryId);
    }

    private sealed record PagedResultDto<T>(IReadOnlyCollection<T> Items, int TotalCount, int PageNumber, int PageSize);
}
