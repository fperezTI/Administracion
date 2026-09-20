using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Reports;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Maintenance;
using AssetManagement.Domain.Organization;
using AssetManagement.Domain.SparePartsAndConsumables;
using AssetManagement.Infrastructure.Persistence;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>F10's reporting queries against real SQL Server — data is seeded directly through
/// <c>AppDbContext</c> (the write-side commands that produce this data are already covered by their own
/// phases' integration tests; this file exercises the read-side aggregation itself, per-company and
/// consolidated across two companies).</summary>
public class ReportsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Reports_scoped_to_one_company_only_see_that_companys_data()
    {
        var (client, companyA, companyB, categoryId) = await ArrangeTwoCompaniesAsync(
            "Reports.Read", "Reports.ReadConsolidated", "Reports.Export");

        await SeedReportDataAsync(companyA, categoryId, assetSuffix: "A");
        await SeedReportDataAsync(companyB, categoryId, assetSuffix: "B");

        var inventory = await client.GetFromJsonAsync<InventorySummaryResult>($"/api/v1/reports/inventory-summary?companyId={companyA}", JsonOptions);
        inventory!.TotalAssets.Should().Be(1);

        var warranties = await client.GetFromJsonAsync<List<ExpiringWarrantyRow>>(
            $"/api/v1/reports/expiring-warranties?companyId={companyA}&withinDays=30", JsonOptions);
        warranties.Should().ContainSingle();

        var lowStock = await client.GetFromJsonAsync<List<LowStockConsumableRow>>(
            $"/api/v1/reports/low-stock-consumables?companyId={companyA}", JsonOptions);
        lowStock.Should().ContainSingle();

        var kpis = await client.GetFromJsonAsync<MaintenanceKpisResult>($"/api/v1/reports/maintenance-kpis?companyId={companyA}", JsonOptions);
        kpis!.ClosedOrdersCount.Should().Be(1);
    }

    [Fact]
    public async Task Consolidated_reports_aggregate_both_companies()
    {
        var (client, companyA, companyB, categoryId) = await ArrangeTwoCompaniesAsync(
            "Reports.Read", "Reports.ReadConsolidated", "Reports.Export");

        await SeedReportDataAsync(companyA, categoryId, assetSuffix: "A");
        await SeedReportDataAsync(companyB, categoryId, assetSuffix: "B");

        var inventory = await client.GetFromJsonAsync<InventorySummaryResult>("/api/v1/reports/inventory-summary", JsonOptions);
        inventory!.TotalAssets.Should().Be(2);

        var warranties = await client.GetFromJsonAsync<List<ExpiringWarrantyRow>>("/api/v1/reports/expiring-warranties?withinDays=30", JsonOptions);
        warranties.Should().HaveCount(2);
    }

    [Fact]
    public async Task Export_endpoints_return_valid_xlsx_and_pdf_files()
    {
        var (client, companyA, _, categoryId) = await ArrangeTwoCompaniesAsync("Reports.Read", "Reports.ReadConsolidated", "Reports.Export");
        await SeedReportDataAsync(companyA, categoryId, assetSuffix: "A");

        var xlsxResponse = await client.GetAsync($"/api/v1/reports/low-stock-consumables/export?companyId={companyA}&format=Xlsx");
        xlsxResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var workbook = new XLWorkbook(new MemoryStream(await xlsxResponse.Content.ReadAsByteArrayAsync()));
        workbook.Worksheet(1).RowsUsed().Count().Should().Be(2); // header + 1 row

        var pdfResponse = await client.GetAsync($"/api/v1/reports/expiring-warranties/export?companyId={companyA}&withinDays=30&format=Pdf");
        pdfResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        Encoding.ASCII.GetString(pdfBytes, 0, 4).Should().Be("%PDF");
    }

    private async Task SeedReportDataAsync(Guid companyId, Guid categoryId, string assetSuffix)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var asset = Asset.Create(companyId, categoryId, $"ASSET-RPT-{assetSuffix}", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null);
        db.Assets.Add(asset);

        var warranty = Warranty.Create(
            companyId, asset.Id, WarrantyType.Manufacturer, "Dell",
            DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-300), DateOnly.FromDateTime(Now.UtcDateTime).AddDays(10), null, Now, null);
        db.Warranties.Add(warranty);

        var consumable = Consumable.Create(companyId, $"Toner {assetSuffix}", null, "Pieza", 10, Now, null);
        consumable.ApplyStockMovement(ConsumableStockDirection.In, 2, Now, null);
        db.Consumables.Add(consumable);

        var order = MaintenanceOrder.Open(companyId, asset.Id, $"MAINT-RPT-{assetSuffix}", MaintenanceOrderType.Corrective, "Falla", null, null, null, Now, null);
        order.Close(AssetStatus.InWarehouse, "Reparado.", null, Now.AddHours(3), null);
        db.MaintenanceOrders.Add(order);

        await db.SaveChangesAsync();
    }

    private async Task<(HttpClient Client, Guid CompanyA, Guid CompanyB, Guid CategoryId)> ArrangeTwoCompaniesAsync(params string[] permissionCodes)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Reports Tester");
        client.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyAId;
        Guid companyBId;
        Guid categoryId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);

            var companyA = Company.Create($"Reportes A {entraObjectId}", "Empresa A", $"TAX-RPT-A-{entraObjectId}", "MXN", "America/Mexico_City", Now);
            var companyB = Company.Create($"Reportes B {entraObjectId}", "Empresa B", $"TAX-RPT-B-{entraObjectId}", "MXN", "America/Mexico_City", Now);
            db.Companies.AddRange(companyA, companyB);
            await db.SaveChangesAsync();
            companyAId = companyA.Id;
            companyBId = companyB.Id;

            user.GrantCompanyAccess(companyAId, Now);
            user.GrantCompanyAccess(companyBId, Now);

            var permissionIds = await db.Permissions
                .Where(p => permissionCodes.Contains(p.Module + "." + p.Action))
                .Select(p => p.Id)
                .ToListAsync();
            var role = Role.Create($"Rol Reportes {entraObjectId}", null, Now);
            role.SetPermissions(permissionIds);
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            user.AssignRole(role.Id, null, Now);
            await db.SaveChangesAsync();

            categoryId = await db.AssetCategories.Select(c => c.Id).FirstAsync();
        }

        return (client, companyAId, companyBId, categoryId);
    }
}
