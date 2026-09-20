using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.ImportExport;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.ImportExport;
using AssetManagement.Domain.Organization;
using AssetManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>F9's asynchronous import flow — the real <c>ImportBatchBackgroundService</c> runs inside this
/// host (Docker's <c>Channel&lt;T&gt;</c>-backed <c>IImportQueue</c>, per ADR 0003/0011), so these tests
/// poll for the status a real user's browser would also have to wait for.</summary>
public class ImportsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task Uploading_validates_asynchronously_and_reports_valid_and_invalid_rows()
    {
        var (client, companyId, categoryCode) = await ArrangeUserAsync("Imports.Read", "Imports.Create");
        var csv = BuildCsv(
            $"{categoryCode},Dell,Latitude 5420,IMP-SN-001,,Excellent,,",
            $"{categoryCode},HP,EliteBook 840,IMP-SN-002,,Good,,",
            ",Lenovo,ThinkPad,IMP-SN-003,,Good,,"); // missing category code -> invalid

        var batchId = await UploadAsync(client, companyId, csv);
        var detail = await PollUntilAsync(client, batchId, d => d.Status != ImportBatchStatus.Queued && d.Status != ImportBatchStatus.Validating);

        detail.Status.Should().Be(ImportBatchStatus.Validated);
        detail.TotalRows.Should().Be(3);
        detail.ValidRows.Should().Be(2);
        detail.InvalidRows.Should().Be(1);
        detail.Rows.Should().HaveCount(3);
        detail.Rows.Count(r => r.Success).Should().Be(2);
    }

    [Fact]
    public async Task Committing_ValidRowsOnly_creates_only_the_valid_assets()
    {
        var (client, companyId, categoryCode) = await ArrangeUserAsync("Imports.Read", "Imports.Create");
        var csv = BuildCsv(
            $"{categoryCode},Dell,Latitude 5420,IMP-SN-101,,Excellent,,",
            ",HP,EliteBook 840,IMP-SN-102,,Good,,");

        var batchId = await UploadAsync(client, companyId, csv);
        await PollUntilAsync(client, batchId, d => d.Status == ImportBatchStatus.Validated);

        var commitResponse = await client.PostAsJsonAsync(
            $"/api/v1/import-batches/{batchId}/commit", new { mode = "ValidRowsOnly" }, JsonOptions);
        commitResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await PollUntilAsync(
            client, batchId, d => d.Status is ImportBatchStatus.Completed or ImportBatchStatus.CompletedWithErrors);

        detail.Status.Should().Be(ImportBatchStatus.CompletedWithErrors);
        detail.SucceededRows.Should().Be(1);
        detail.FailedRows.Should().Be(1);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var created = await db.Assets.IgnoreQueryFilters().Where(a => a.CompanyId == companyId).ToListAsync();
        created.Should().ContainSingle(a => a.SerialNumber == "IMP-SN-101");
    }

    [Fact]
    public async Task Committing_AllOrNothing_is_rejected_up_front_while_rows_are_still_invalid()
    {
        var (client, companyId, categoryCode) = await ArrangeUserAsync("Imports.Read", "Imports.Create");
        var csv = BuildCsv(
            $"{categoryCode},Dell,Latitude 5420,IMP-SN-201,,Excellent,,",
            ",HP,EliteBook 840,IMP-SN-202,,Good,,");

        var batchId = await UploadAsync(client, companyId, csv);
        await PollUntilAsync(client, batchId, d => d.Status == ImportBatchStatus.Validated);

        var commitResponse = await client.PostAsJsonAsync(
            $"/api/v1/import-batches/{batchId}/commit", new { mode = "AllOrNothing" }, JsonOptions);

        commitResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Assets.IgnoreQueryFilters().CountAsync(a => a.CompanyId == companyId)).Should().Be(0);
    }

    [Fact]
    public async Task Cancelling_before_commit_leaves_the_batch_cancelled_with_no_assets_created()
    {
        var (client, companyId, categoryCode) = await ArrangeUserAsync("Imports.Read", "Imports.Create");
        var csv = BuildCsv($"{categoryCode},Dell,Latitude 5420,IMP-SN-301,,Excellent,,");

        var batchId = await UploadAsync(client, companyId, csv);
        await PollUntilAsync(client, batchId, d => d.Status == ImportBatchStatus.Validated);

        var cancelResponse = await client.PostAsync($"/api/v1/import-batches/{batchId}/cancel", content: null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await GetDetailAsync(client, batchId);
        detail.Status.Should().Be(ImportBatchStatus.Cancelled);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Assets.IgnoreQueryFilters().CountAsync(a => a.CompanyId == companyId)).Should().Be(0);
    }

    private static string BuildCsv(params string[] dataRows)
    {
        var header = string.Join(',', AssetImportColumns.FixedColumns);
        return string.Join("\r\n", new[] { header }.Concat(dataRows)) + "\r\n";
    }

    private static async Task<Guid> UploadAsync(HttpClient client, Guid companyId, string csv)
    {
        using var form = new MultipartFormDataContent { { new StringContent(companyId.ToString()), "companyId" } };
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "file", "activos.csv");

        var response = await client.PostAsync("/api/v1/import-batches", form);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>(JsonOptions);
    }

    private static async Task<ImportBatchDetail> GetDetailAsync(HttpClient client, Guid batchId) =>
        (await client.GetFromJsonAsync<ImportBatchDetail>($"/api/v1/import-batches/{batchId}", JsonOptions))!;

    private static async Task<ImportBatchDetail> PollUntilAsync(
        HttpClient client, Guid batchId, Func<ImportBatchDetail, bool> isDone, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
        while (true)
        {
            var detail = await GetDetailAsync(client, batchId);
            if (isDone(detail))
            {
                return detail;
            }

            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"El lote {batchId} no alcanzó el estado esperado a tiempo (estado actual: {detail.Status}).");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }
    }

    private async Task<(HttpClient Client, Guid CompanyId, string CategoryCode)> ArrangeUserAsync(params string[] permissionCodes)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Imports Tester");
        client.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyId;
        string categoryCode;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);

            var company = Company.Create(
                $"Importaciones {entraObjectId} S.A.", "Empresa Importaciones", $"TAX-IMP-{entraObjectId}", "MXN",
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

            categoryCode = await db.AssetCategories.Select(c => c.Code).FirstAsync();
        }

        return (client, companyId, categoryCode);
    }
}
