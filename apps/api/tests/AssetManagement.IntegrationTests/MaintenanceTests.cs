using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Identity;
using AssetManagement.Application.Maintenance;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Maintenance;
using AssetManagement.Domain.Organization;
using AssetManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>
/// F6's core maintenance flow — open an order with a checklist (moves the asset to InMaintenance), close it
/// with a result and checklist results (moves the asset to the closing result), and confirm closing a
/// maintenance order can never target PendingDecommission directly (see the F6 plan, decision 3).
/// </summary>
public class MaintenanceTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Opening_and_closing_an_order_with_a_checklist_moves_the_asset_through_InMaintenance()
    {
        var (client, companyId) = await ArrangeUserAsync(
            "Assets.Create", "Assets.Read", "Catalogs.Read", "Maintenance.Create", "Maintenance.Read", "Maintenance.Update");

        var assetId = await CreateAssetAsync(client, companyId);

        var checklistResponse = await client.PostAsJsonAsync(
            "/api/v1/maintenance-checklists",
            new { key = $"pm-laptop-{Guid.NewGuid()}", name = "PM Laptop", assetCategoryId = (Guid?)null, initialItems = new[] { "Limpiar ventiladores", "Actualizar BIOS" } });
        checklistResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var checklistId = await checklistResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var openResponse = await client.PostAsJsonAsync(
            "/api/v1/maintenance-orders",
            new { assetId, type = "Preventive", description = "Mantenimiento preventivo semestral", checklistDefinitionId = checklistId });
        openResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = await openResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        (await client.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions))!
            .Status.Should().Be(AssetStatus.InMaintenance);

        var orderAfterOpen = await client.GetFromJsonAsync<MaintenanceOrderDetail>($"/api/v1/maintenance-orders/{orderId}", JsonOptions);
        orderAfterOpen!.ChecklistResults.Should().HaveCount(2);
        orderAfterOpen.ChecklistResults.Should().OnlyContain(r => !r.IsCompleted);

        var closeResponse = await client.PostAsJsonAsync(
            $"/api/v1/maintenance-orders/{orderId}/close",
            new
            {
                resultStatus = "InWarehouse",
                resultNotes = "Se limpiaron ventiladores y actualizó BIOS.",
                checklistItemResults = new[]
                {
                    new { itemIndex = 0, isCompleted = true, notes = (string?)null },
                    new { itemIndex = 1, isCompleted = true, notes = (string?)"v2.14.0" },
                },
            });
        closeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await client.GetFromJsonAsync<AssetDetail>($"/api/v1/assets/{assetId}", JsonOptions))!
            .Status.Should().Be(AssetStatus.InWarehouse);

        var orderAfterClose = await client.GetFromJsonAsync<MaintenanceOrderDetail>($"/api/v1/maintenance-orders/{orderId}", JsonOptions);
        orderAfterClose!.Status.Should().Be(MaintenanceOrderStatus.Closed);
        orderAfterClose.ChecklistResults.Should().OnlyContain(r => r.IsCompleted);
    }

    [Fact]
    public async Task Closing_an_order_with_PendingDecommission_as_the_result_is_rejected()
    {
        var (client, companyId) = await ArrangeUserAsync(
            "Assets.Create", "Assets.Read", "Catalogs.Read", "Maintenance.Create", "Maintenance.Read", "Maintenance.Update");

        var assetId = await CreateAssetAsync(client, companyId);
        var openResponse = await client.PostAsJsonAsync(
            "/api/v1/maintenance-orders",
            new { assetId, type = "Corrective", description = "No enciende", checklistDefinitionId = (Guid?)null });
        var orderId = await openResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var closeResponse = await client.PostAsJsonAsync(
            $"/api/v1/maintenance-orders/{orderId}/close",
            new { resultStatus = "PendingDecommission", resultNotes = "No se pudo reparar.", checklistItemResults = (object?)null });

        closeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    private async Task<(HttpClient Client, Guid CompanyId)> ArrangeUserAsync(params string[] permissionCodes)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Maintenance Tester");
        client.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);

            var company = Company.Create(
                $"Mantenimiento {entraObjectId} S.A.", "Empresa Mantenimiento", $"TAX-MAINT-{entraObjectId}", "MXN",
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
}
