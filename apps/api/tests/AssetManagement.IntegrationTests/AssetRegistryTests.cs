using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Assets.Categories;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Organization;
using AssetManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>Covers the "alta y etiquetado de un activo" mandatory E2E flow (pedido §35) over the real
/// HTTP pipeline: authentication, RBAC, folio generation (the real EfFolioGenerator — InMemory can't run
/// its SQL Server MERGE, so this is the only place it's actually exercised), and multi-company
/// scoping.</summary>
public class AssetRegistryTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    // The API serializes enums as strings (Program.cs); System.Net.Http.Json's ReadFromJsonAsync uses
    // its own default options unless given these explicitly.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Creating_an_asset_without_the_required_permission_is_forbidden()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync("/api/v1/assets", new
        {
            companyId = Guid.NewGuid(),
            assetCategoryId = Guid.NewGuid(),
            brand = "Dell",
            model = "Latitude",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Creates_an_asset_end_to_end_with_a_generated_folio_and_tag()
    {
        var (client, companyId, categoryId) = await ArrangeAuthenticatedClientAsync(
            "Assets.Create", "Assets.Read", "Catalogs.Read");

        var createResponse = await client.PostAsJsonAsync("/api/v1/assets", new
        {
            companyId,
            assetCategoryId = categoryId,
            brand = "Dell",
            model = "Latitude 5450",
            serialNumber = "SN-E2E-1",
            description = "Laptop de prueba end-to-end",
            physicalCondition = "Excellent",
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAssetResult>(JsonOptions);
        created!.InternalFolio.Should().MatchRegex(@"^ASSET-\d{6}$");
        created.TagCode.Should().NotBeNullOrWhiteSpace();

        var getResponse = await client.GetAsync($"/api/v1/assets/{created.AssetId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await getResponse.Content.ReadFromJsonAsync<AssetDetail>(JsonOptions);
        detail!.Status.Should().Be(AssetStatus.InWarehouse);
        detail.Tag!.Code.Should().Be(created.TagCode);

        var listResponse = await client.GetAsync($"/api/v1/assets?companyId={companyId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResultDto<AssetSummary>>(JsonOptions);
        page!.Items.Should().ContainSingle(a => a.Id == created.AssetId);
    }

    [Fact]
    public async Task Assets_from_a_company_the_caller_does_not_belong_to_are_not_returned()
    {
        var (client, ownCompanyId, categoryId) = await ArrangeAuthenticatedClientAsync("Assets.Create", "Assets.Read");

        var created = await client.PostAsJsonAsync("/api/v1/assets", new
        {
            companyId = ownCompanyId,
            assetCategoryId = categoryId,
            brand = "Dell",
            model = "Latitude",
            physicalCondition = "Good",
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var otherCompanyId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/v1/assets?companyId={otherCompanyId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<(HttpClient Client, Guid CompanyId, Guid CategoryId)> ArrangeAuthenticatedClientAsync(
        params string[] permissionCodes)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Asset Tester");
        client.DefaultRequestHeaders.Add("Test-Email", "asset-tester@example.com");

        // Provision the user first (first authenticated call).
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
