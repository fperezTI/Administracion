using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Organization;
using AssetManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace AssetManagement.IntegrationTests;

/// <summary>
/// F12's load testing ("k6 o equivalente" — docs/testing.md already allowed an equivalent). Runs real
/// concurrent HTTP traffic through <see cref="ApiWebApplicationFactory"/> (the same real SQL Server via
/// Testcontainers every other integration test uses, not a fake) against the read/write scenarios
/// closest to pedido §35's critical flows, seeded with ~2,000 real assets.
///
/// <b>Explicit scope limitation</b> (see docs/performance.md): this validates behavior under moderate
/// concurrency on a single local Docker Compose/Testcontainers instance — it does **not** validate the
/// pedido's full reference scale (250k activos, 2M movimientos, 500 usuarios concurrentes). That requires
/// real Azure infrastructure with proper connection-pool/scaling tuning and is explicitly deferred to F13
/// — `docs/testing.md` already anticipated this ("ejecutado en fase F12 contra ambiente `test`", an Azure
/// environment that doesn't exist yet).
/// </summary>
public class LoadTests(ApiWebApplicationFactory factory, ITestOutputHelper output) : IClassFixture<ApiWebApplicationFactory>
{
    private const int SeedAssetCount = 2000;
    private const int ReadConcurrency = 50;
    private const int ReadIterationsPerUser = 5;
    private const int WriteConcurrency = 20;

    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task Concurrent_reads_and_writes_complete_within_generous_latency_thresholds_with_no_errors()
    {
        var (companyId, categoryId, assetIds) = await SeedAsync();
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Load Tester");
        client.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");
        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();
        await GrantFullAccessAsync(entraObjectId, companyId);

        var listAssets = await RunConcurrentAsync(ReadConcurrency, ReadIterationsPerUser,
            _ => client.GetAsync($"/api/v1/assets?companyId={companyId}&pageNumber=1&pageSize=50"));
        Report("Listar activos (paginado)", listAssets);

        var random = new Random(42);
        var getAssetById = await RunConcurrentAsync(ReadConcurrency, ReadIterationsPerUser,
            _ => client.GetAsync($"/api/v1/assets/{assetIds[random.Next(assetIds.Count)]}"));
        Report("Detalle de activo", getAssetById);

        var search = await RunConcurrentAsync(ReadConcurrency, ReadIterationsPerUser,
            _ => client.GetAsync($"/api/v1/search?term=ASSET-LOAD&companyId={companyId}"));
        Report("Búsqueda global", search);

        var inventorySummary = await RunConcurrentAsync(ReadConcurrency, ReadIterationsPerUser,
            _ => client.GetAsync($"/api/v1/reports/inventory-summary?companyId={companyId}"));
        Report("Resumen de inventario", inventorySummary);

        // The interesting one: concurrent asset creation stresses EfFolioGenerator's atomic MERGE +
        // optimistic-retry (pedido C8) under genuine write contention for the first time in this project.
        var createAssets = await RunConcurrentAsync(WriteConcurrency, iterationsPerUser: 1, async i =>
            await client.PostAsJsonAsync("/api/v1/assets", new
            {
                companyId,
                assetCategoryId = categoryId,
                brand = "LoadTest",
                model = $"Concurrent-{i}",
                physicalCondition = "Good",
            }));
        Report("Alta de activos concurrente", createAssets);

        AssertHealthy(listAssets, maxP95Ms: 3000);
        AssertHealthy(getAssetById, maxP95Ms: 3000);
        AssertHealthy(search, maxP95Ms: 3000);
        AssertHealthy(inventorySummary, maxP95Ms: 3000);
        AssertHealthy(createAssets, maxP95Ms: 5000);

        // Folio uniqueness under concurrency: every one of the WriteConcurrency creates must have gotten
        // its own folio — a collision here would mean EfFolioGenerator's retry logic is broken.
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var folios = await db.Assets.IgnoreQueryFilters()
            .Where(a => a.CompanyId == companyId && a.Brand == "LoadTest")
            .Select(a => a.InternalFolio)
            .ToListAsync();
        folios.Should().OnlyHaveUniqueItems();
        folios.Should().HaveCount(WriteConcurrency);
    }

    private static async Task<LoadResult> RunConcurrentAsync(
        int concurrency, int iterationsPerUser, Func<int, Task<HttpResponseMessage>> call)
    {
        var latencies = new System.Collections.Concurrent.ConcurrentBag<double>();
        var errors = new System.Collections.Concurrent.ConcurrentBag<string>();

        var tasks = Enumerable.Range(0, concurrency).Select(async userIndex =>
        {
            for (var iteration = 0; iteration < iterationsPerUser; iteration++)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    using var response = await call(userIndex * iterationsPerUser + iteration);
                    stopwatch.Stop();
                    latencies.Add(stopwatch.Elapsed.TotalMilliseconds);
                    if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Created)
                    {
                        errors.Add($"{(int)response.StatusCode} {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    errors.Add(ex.Message);
                }
            }
        });

        await Task.WhenAll(tasks);

        var sorted = latencies.OrderBy(x => x).ToList();
        return new LoadResult(
            sorted.Count,
            errors.ToList(),
            Percentile(sorted, 0.50),
            Percentile(sorted, 0.95),
            Percentile(sorted, 0.99));
    }

    private static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        return sortedValues[Math.Clamp(index, 0, sortedValues.Count - 1)];
    }

    private void Report(string scenario, LoadResult result)
    {
        output.WriteLine(
            $"{scenario}: {result.RequestCount} solicitudes, p50={result.P50Ms:F0}ms p95={result.P95Ms:F0}ms " +
            $"p99={result.P99Ms:F0}ms errores={result.Errors.Count}");
        foreach (var error in result.Errors.Take(5))
        {
            output.WriteLine($"  error: {error}");
        }
    }

    private static void AssertHealthy(LoadResult result, double maxP95Ms)
    {
        result.Errors.Should().BeEmpty($"no debería haber errores inesperados bajo esta carga (vistos: {string.Join(", ", result.Errors.Take(3))})");
        result.P95Ms.Should().BeLessThan(maxP95Ms, "la latencia p95 no debería degradarse más allá de un umbral generoso en este entorno local");
    }

    private async Task<(Guid CompanyId, Guid CategoryId, IReadOnlyList<Guid> AssetIds)> SeedAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var company = Company.Create(
            $"Carga {Guid.NewGuid()}", "Empresa Carga", $"TAX-LOAD-{Guid.NewGuid()}", "MXN", "America/Mexico_City", Now);
        db.Companies.Add(company);
        var category = AssetCategory.Create("Carga", $"LOAD-{Guid.NewGuid():N}"[..12], IdentificationTechnology.Qr, Now);
        db.AssetCategories.Add(category);
        await db.SaveChangesAsync();

        var assets = Enumerable.Range(0, SeedAssetCount)
            .Select(i => Asset.Create(
                company.Id, category.Id, $"ASSET-LOAD-{i:D6}", "Dell", "Latitude", $"SN-LOAD-{i:D6}", null,
                PhysicalCondition.Good, Now, null))
            .ToList();
        db.Assets.AddRange(assets);
        await db.SaveChangesAsync();

        return (company.Id, category.Id, assets.Select(a => a.Id).ToList());
    }

    private async Task GrantFullAccessAsync(Guid entraObjectId, Guid companyId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);
        user.GrantCompanyAccess(companyId, Now);

        var permissionIds = await db.Permissions
            .Where(p => new[] { "Assets.Read", "Assets.Create", "Catalogs.Read", "Movements.Read", "Requests.Read", "Reports.Read" }
                .Contains(p.Module + "." + p.Action))
            .Select(p => p.Id)
            .ToListAsync();
        var role = Role.Create($"Rol Carga {entraObjectId}", null, Now);
        role.SetPermissions(permissionIds);
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        user.AssignRole(role.Id, null, Now);
        await db.SaveChangesAsync();
    }

    private sealed record LoadResult(int RequestCount, IReadOnlyList<string> Errors, double P50Ms, double P95Ms, double P99Ms);
}
