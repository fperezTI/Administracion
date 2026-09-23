using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Organization;
using AssetManagement.Application.SparePartsAndConsumables;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Organization;
using AssetManagement.Domain.SparePartsAndConsumables;
using AssetManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>F6's spare parts (serialized, install/uninstall cycle) and consumables (existence tracked only
/// through immutable stock movements) flows, over the real HTTP pipeline against SQL Server.</summary>
public class SparePartsAndConsumablesTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Installing_and_uninstalling_a_spare_part_tracks_its_history()
    {
        var (client, companyId, warehouseId) = await ArrangeUserAsync(
            "Assets.Create", "Assets.Read", "Catalogs.Read", "SpareParts.Create", "SpareParts.Read", "SpareParts.Update");

        var assetId = await CreateAssetAsync(client, companyId);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/spare-parts",
            new { companyId, name = "Memoria RAM 16GB", partNumber = "RAM-16GB", serialNumber = $"SN-{Guid.NewGuid()}", warehouseOrgUnitId = warehouseId });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var sparePartId = await createResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var installResponse = await client.PostAsJsonAsync(
            $"/api/v1/spare-parts/{sparePartId}/install", new { assetId, maintenanceOrderId = (Guid?)null });
        installResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterInstall = await client.GetFromJsonAsync<SparePartDetail>($"/api/v1/spare-parts/{sparePartId}", JsonOptions);
        afterInstall!.Status.Should().Be(SparePartStatus.Installed);
        afterInstall.CurrentAssetId.Should().Be(assetId);

        var uninstallResponse = await client.PostAsJsonAsync(
            $"/api/v1/spare-parts/{sparePartId}/uninstall", new { warehouseOrgUnitId = warehouseId });
        uninstallResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterUninstall = await client.GetFromJsonAsync<SparePartDetail>($"/api/v1/spare-parts/{sparePartId}", JsonOptions);
        afterUninstall!.Status.Should().Be(SparePartStatus.InStock);
        afterUninstall.Installations.Should().ContainSingle(i => i.AssetId == assetId && i.RemovedAtUtc != null);
    }

    [Fact]
    public async Task Registering_stock_movements_updates_the_consumables_current_stock()
    {
        var (client, companyId, warehouseId) = await ArrangeUserAsync(
            "Consumables.Create", "Consumables.Read", "Consumables.Update");

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/consumables", new { companyId, name = "Tóner HP 58A", sku = "TN-58A", unitOfMeasure = "Pieza", minimumStock = 2 });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var consumableId = await createResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var inResponse = await client.PostAsJsonAsync(
            $"/api/v1/consumables/{consumableId}/movements",
            new { warehouseOrgUnitId = warehouseId, direction = "In", reason = "Purchase", quantity = 10, referenceMaintenanceOrderId = (Guid?)null, notes = "Compra inicial" });
        inResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        (await client.GetFromJsonAsync<PagedResult<ConsumableSummary>>($"/api/v1/consumables?companyId={companyId}", JsonOptions))!
            .Items.Single(c => c.Id == consumableId).CurrentStock.Should().Be(10);

        var outResponse = await client.PostAsJsonAsync(
            $"/api/v1/consumables/{consumableId}/movements",
            new { warehouseOrgUnitId = warehouseId, direction = "Out", reason = "Consumption", quantity = 3, referenceMaintenanceOrderId = (Guid?)null, notes = (string?)null });
        outResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        (await client.GetFromJsonAsync<PagedResult<ConsumableSummary>>($"/api/v1/consumables?companyId={companyId}", JsonOptions))!
            .Items.Single(c => c.Id == consumableId).CurrentStock.Should().Be(7);

        var movements = await client.GetFromJsonAsync<List<ConsumableStockMovementSummary>>($"/api/v1/consumables/{consumableId}/movements", JsonOptions);
        movements.Should().HaveCount(2);
    }

    [Fact]
    public async Task A_movement_that_would_leave_stock_negative_is_rejected()
    {
        var (client, companyId, warehouseId) = await ArrangeUserAsync("Consumables.Create", "Consumables.Read", "Consumables.Update");

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/consumables", new { companyId, name = "Cable HDMI", sku = (string?)null, unitOfMeasure = "Pieza", minimumStock = (decimal?)null });
        var consumableId = await createResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        await client.PostAsJsonAsync(
            $"/api/v1/consumables/{consumableId}/movements",
            new { warehouseOrgUnitId = warehouseId, direction = "In", reason = "Purchase", quantity = 2, referenceMaintenanceOrderId = (Guid?)null, notes = (string?)null });

        var overDrawResponse = await client.PostAsJsonAsync(
            $"/api/v1/consumables/{consumableId}/movements",
            new { warehouseOrgUnitId = warehouseId, direction = "Out", reason = "Consumption", quantity = 5, referenceMaintenanceOrderId = (Guid?)null, notes = (string?)null });

        overDrawResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        (await client.GetFromJsonAsync<PagedResult<ConsumableSummary>>($"/api/v1/consumables?companyId={companyId}", JsonOptions))!
            .Items.Single(c => c.Id == consumableId).CurrentStock.Should().Be(2);
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

    private async Task<(HttpClient Client, Guid CompanyId, Guid WarehouseOrgUnitId)> ArrangeUserAsync(params string[] permissionCodes)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Spare Parts Tester");
        client.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyId;
        Guid warehouseId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);

            var company = Company.Create(
                $"Refacciones {entraObjectId} S.A.", "Empresa Refacciones", $"TAX-SP-{entraObjectId}", "MXN",
                "America/Mexico_City", DateTimeOffset.UtcNow);
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            companyId = company.Id;

            user.GrantCompanyAccess(companyId, DateTimeOffset.UtcNow);

            var warehouseTypeId = await db.OrgUnitTypes.Where(t => t.Code == OrgUnitTypeCatalog.Codes.Warehouse).Select(t => t.Id).SingleAsync();
            var warehouse = OrgUnit.Create(companyId, warehouseTypeId, null, "Almacén Central", "ALM-01", DateTimeOffset.UtcNow);
            db.OrgUnits.Add(warehouse);
            await db.SaveChangesAsync();
            warehouseId = warehouse.Id;

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

        return (client, companyId, warehouseId);
    }
}
