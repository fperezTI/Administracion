using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Search;
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

/// <summary>F11's global search against real SQL Server — permission filtering is real (via the same
/// <c>EfPermissionChecker</c>/role assignment the rest of the app uses, not a fake), and company scoping
/// is verified across two real companies.</summary>
public class SearchTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Search_finds_an_asset_by_partial_folio_but_hides_warranties_without_that_permission()
    {
        var (client, companyId, _) = await ArrangeAsync("Assets.Read"); // no Warranties.Read
        var (assetId, _) = await SeedAssetAndWarrantyAsync(companyId);

        var response = await client.GetFromJsonAsync<List<SearchResultItem>>(
            $"/api/v1/search?term=SRCH-UNIQUE&companyId={companyId}", JsonOptions);

        response.Should().ContainSingle(r => r.EntityType == "Asset" && r.EntityId == assetId);
        response.Should().NotContain(r => r.EntityType == "Warranty");
    }

    [Fact]
    public async Task Search_includes_warranties_once_the_caller_has_Warranties_Read()
    {
        var (client, companyId, _) = await ArrangeAsync("Assets.Read", "Warranties.Read");
        var (_, warrantyId) = await SeedAssetAndWarrantyAsync(companyId);

        var response = await client.GetFromJsonAsync<List<SearchResultItem>>(
            $"/api/v1/search?term=ProveedorSRCHUnique&companyId={companyId}", JsonOptions);

        response.Should().ContainSingle(r => r.EntityType == "Warranty" && r.EntityId == warrantyId);
        response!.Single(r => r.EntityType == "Warranty").LinkEntityType.Should().Be("Warranty");
    }

    [Fact]
    public async Task Search_scoped_to_one_company_excludes_the_other_companys_asset()
    {
        var (client, companyA, companyB) = await ArrangeAsync("Assets.Read");

        Guid categoryId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            categoryId = await db.AssetCategories.Select(c => c.Id).FirstAsync();
            db.Assets.Add(Asset.Create(companyA, categoryId, "SRCH-A-ONLY", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null));
            db.Assets.Add(Asset.Create(companyB, categoryId, "SRCH-B-ONLY", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null));
            await db.SaveChangesAsync();
        }

        var scopedToA = await client.GetFromJsonAsync<List<SearchResultItem>>($"/api/v1/search?term=SRCH-&companyId={companyA}", JsonOptions);
        var consolidated = await client.GetFromJsonAsync<List<SearchResultItem>>("/api/v1/search?term=SRCH-", JsonOptions);

        scopedToA.Should().ContainSingle(r => r.Title == "SRCH-A-ONLY");
        consolidated.Should().Contain(r => r.Title == "SRCH-A-ONLY").And.Contain(r => r.Title == "SRCH-B-ONLY");
    }

    private async Task<(Guid AssetId, Guid WarrantyId)> SeedAssetAndWarrantyAsync(Guid companyId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoryId = await db.AssetCategories.Select(c => c.Id).FirstAsync();

        var asset = Asset.Create(companyId, categoryId, "ASSET-SRCH-UNIQUE-1", "Dell", "Latitude", null, null, PhysicalCondition.Good, Now, null);
        db.Assets.Add(asset);

        var warranty = Warranty.Create(
            companyId, asset.Id, WarrantyType.Manufacturer, "ProveedorSRCHUnique",
            DateOnly.FromDateTime(Now.UtcDateTime), DateOnly.FromDateTime(Now.UtcDateTime).AddYears(1), null, Now, null);
        db.Warranties.Add(warranty);

        await db.SaveChangesAsync();
        return (asset.Id, warranty.Id);
    }

    private async Task<(HttpClient Client, Guid CompanyA, Guid CompanyB)> ArrangeAsync(params string[] permissionCodes)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Search Tester");
        client.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyAId;
        Guid companyBId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);

            var companyA = Company.Create($"Búsqueda A {entraObjectId}", "Empresa A", $"TAX-SRCH-A-{entraObjectId}", "MXN", "America/Mexico_City", Now);
            var companyB = Company.Create($"Búsqueda B {entraObjectId}", "Empresa B", $"TAX-SRCH-B-{entraObjectId}", "MXN", "America/Mexico_City", Now);
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
            var role = Role.Create($"Rol Búsqueda {entraObjectId}", null, Now);
            role.SetPermissions(permissionIds);
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            user.AssignRole(role.Id, null, Now);
            await db.SaveChangesAsync();
        }

        return (client, companyAId, companyBId);
    }
}
