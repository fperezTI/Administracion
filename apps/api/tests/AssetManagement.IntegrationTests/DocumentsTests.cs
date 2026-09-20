using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Assets;
using AssetManagement.Application.Documents;
using AssetManagement.Application.Identity;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Organization;
using AssetManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>F8's document upload/download flow against real Azurite (Testcontainers), not a fake
/// <c>IFileStorage</c> — same rigor the rest of this suite already applies to SQL Server.</summary>
public class DocumentsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Uploading_and_downloading_a_document_round_trips_the_content()
    {
        var (client, companyId) = await ArrangeUserAsync("Assets.Create", "Assets.Read", "Catalogs.Read", "Documents.Create", "Documents.Read");
        var assetId = await CreateAssetAsync(client, companyId);

        var fileBytes = Encoding.UTF8.GetBytes("Contenido de prueba del manual.");
        using var form = new MultipartFormDataContent
        {
            { new StringContent("Asset"), "entityType" },
            { new StringContent(assetId.ToString()), "entityId" },
        };
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(fileContent, "file", "manual.txt");

        var uploadResponse = await client.PostAsync("/api/v1/documents", form);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var documentId = await uploadResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        var listResponse = await client.GetFromJsonAsync<List<DocumentSummary>>(
            $"/api/v1/documents?entityType=Asset&entityId={assetId}", JsonOptions);
        listResponse.Should().ContainSingle(d => d.Id == documentId && d.FileName == "manual.txt");

        var downloadResponse = await client.GetAsync($"/api/v1/documents/{documentId}/content");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var downloadedBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        downloadedBytes.Should().BeEquivalentTo(fileBytes);
    }

    [Fact]
    public async Task Uploading_for_an_entity_type_outside_the_allowlist_is_rejected()
    {
        var (client, companyId) = await ArrangeUserAsync("Documents.Create", "Documents.Read");

        using var form = new MultipartFormDataContent
        {
            { new StringContent("Company"), "entityType" },
            { new StringContent(companyId.ToString()), "entityId" },
        };
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("x"));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(fileContent, "file", "x.txt");

        var response = await client.PostAsync("/api/v1/documents", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
        client.DefaultRequestHeaders.Add("Test-Name", "Documents Tester");
        client.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);

            var company = Company.Create(
                $"Documentos {entraObjectId} S.A.", "Empresa Documentos", $"TAX-DOC-{entraObjectId}", "MXN",
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
