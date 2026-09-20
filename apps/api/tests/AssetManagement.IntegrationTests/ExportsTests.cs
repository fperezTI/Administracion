using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Assets;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Organization;
using AssetManagement.Infrastructure.Persistence;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetManagement.IntegrationTests;

public class ExportsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Exporting_assets_to_xlsx_returns_a_readable_workbook_with_one_row_per_asset()
    {
        var (client, companyId) = await ArrangeUserAsync("Assets.Create", "Catalogs.Read", "Exports.Create");
        await CreateAssetAsync(client, companyId, "Dell", "SN-EXP-001");
        await CreateAssetAsync(client, companyId, "HP", "SN-EXP-002");

        var response = await client.GetAsync($"/api/v1/exports/assets?companyId={companyId}&format=Xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheet(1);
        worksheet.RowsUsed().Count().Should().Be(3); // 1 header + 2 assets
    }

    [Fact]
    public async Task Exporting_assets_to_pdf_returns_a_non_empty_pdf()
    {
        var (client, companyId) = await ArrangeUserAsync("Assets.Create", "Catalogs.Read", "Exports.Create");
        await CreateAssetAsync(client, companyId, "Dell", "SN-EXP-101");

        var response = await client.GetAsync($"/api/v1/exports/assets?companyId={companyId}&format=Pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    private async Task CreateAssetAsync(HttpClient client, Guid companyId, string brand, string serialNumber)
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
            brand,
            model = "Modelo de prueba",
            serialNumber,
            physicalCondition = "Good",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        await response.Content.ReadFromJsonAsync<CreateAssetResult>(JsonOptions);
    }

    private async Task<(HttpClient Client, Guid CompanyId)> ArrangeUserAsync(params string[] permissionCodes)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Exports Tester");
        client.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);

            var company = Company.Create(
                $"Exportaciones {entraObjectId} S.A.", "Empresa Exportaciones", $"TAX-EXP-{entraObjectId}", "MXN",
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
