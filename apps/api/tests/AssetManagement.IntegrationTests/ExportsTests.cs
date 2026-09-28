using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Identity;
using AssetManagement.Application.Inventory;
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

    [Fact]
    public async Task Exporting_assignments_to_xlsx_returns_a_readable_workbook_with_one_row_per_assignment()
    {
        var (client, companyId) = await ArrangeUserAsync("Assets.Create", "Catalogs.Read", "Exports.Create", "Assignments.Create");
        var assetId = await CreateAssetAsync(client, companyId, "Dell", "SN-ASG-EXP-001");
        var recipientUserId = await ArrangeRecipientAsync(companyId);

        var createAssignment = await client.PostAsJsonAsync("/api/v1/assignments", new
        {
            assetId,
            assignedToUserId = recipientUserId,
            orgUnitId = (Guid?)null,
            notes = "Asignación para exportar",
        });
        createAssignment.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.GetAsync($"/api/v1/exports/assignments?companyId={companyId}&format=Xlsx");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheet(1);
        worksheet.RowsUsed().Count().Should().Be(2); // 1 header + 1 assignment
        worksheet.Cell(2, 12).GetString().Should().Be("Asignación para exportar"); // Notas (última columna)
    }

    [Fact]
    public async Task Exporting_assignments_to_pdf_returns_a_non_empty_pdf()
    {
        var (client, companyId) = await ArrangeUserAsync("Assets.Create", "Catalogs.Read", "Exports.Create", "Assignments.Create");
        var assetId = await CreateAssetAsync(client, companyId, "HP", "SN-ASG-EXP-002");
        var recipientUserId = await ArrangeRecipientAsync(companyId);

        var createAssignment = await client.PostAsJsonAsync("/api/v1/assignments", new
        {
            assetId,
            assignedToUserId = recipientUserId,
            orgUnitId = (Guid?)null,
            notes = (string?)null,
        });
        createAssignment.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await client.GetAsync($"/api/v1/exports/assignments?companyId={companyId}&format=Pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
        Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("%PDF");
    }

    [Fact]
    public async Task Exporting_assignments_with_a_status_filter_excludes_assignments_in_other_statuses()
    {
        var (client, companyId) = await ArrangeUserAsync(
            "Assets.Create", "Catalogs.Read", "Exports.Create", "Assignments.Create", "Assignments.Read");
        var acceptedAssetId = await CreateAssetAsync(client, companyId, "Dell", "SN-ASG-EXP-003");
        var pendingAssetId = await CreateAssetAsync(client, companyId, "Dell", "SN-ASG-EXP-004");
        var recipientUserId = await ArrangeRecipientAsync(companyId);

        foreach (var assetId in new[] { acceptedAssetId, pendingAssetId })
        {
            var create = await client.PostAsJsonAsync("/api/v1/assignments", new
            {
                assetId,
                assignedToUserId = recipientUserId,
                orgUnitId = (Guid?)null,
                notes = (string?)null,
            });
            create.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var response = await client.GetAsync(
            $"/api/v1/exports/assignments?companyId={companyId}&format=Xlsx&status=PendingSignature");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheet(1);
        worksheet.RowsUsed().Count().Should().Be(3); // 1 header + 2 pending assignments (neither was signed)
    }

    private async Task<Guid> ArrangeRecipientAsync(Guid companyId)
    {
        var entraObjectId = Guid.NewGuid();
        var recipientClient = factory.CreateClient();
        recipientClient.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        recipientClient.DefaultRequestHeaders.Add("Test-Name", "Export Recipient");
        recipientClient.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");

        var me = await recipientClient.GetFromJsonAsync<MeResponse>("/api/v1/me", JsonOptions);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.Id == me!.UserId);
        user.GrantCompanyAccess(companyId, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();

        return me!.UserId;
    }

    private async Task<Guid> CreateAssetAsync(HttpClient client, Guid companyId, string brand, string serialNumber)
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
        var result = await response.Content.ReadFromJsonAsync<CreateAssetResult>(JsonOptions);
        return result!.AssetId;
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
