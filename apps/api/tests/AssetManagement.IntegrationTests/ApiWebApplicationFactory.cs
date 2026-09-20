using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;
using Testcontainers.MsSql;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>
/// Boots the real API host against a disposable, containerized SQL Server instance so integration tests
/// exercise the actual EF Core provider instead of an in-memory substitute, with the real Entra ID
/// authentication handler swapped for TestAuthHandler (no Entra ID app registration exists yet — see
/// docs/architecture/00-analysis.md §18). Requires Docker to be running.
/// </summary>
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder().Build();
    // Real Azurite, not a fake IFileStorage — same rigor as SQL Server real vs. InMemory (see F8 plan).
    // Explicit :latest image (same one docker-compose.yml uses) — Testcontainers.Azurite 4.0.0's own
    // default image is old enough that Azure.Storage.Blobs' default service version header
    // ("2024-11-04") isn't recognized, failing every request with a 400.
    private readonly AzuriteContainer _azuriteContainer =
        new AzuriteBuilder().WithImage("mcr.microsoft.com/azure-storage/azurite:latest").Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sqlContainer.StartAsync(), _azuriteContainer.StartAsync());

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;
        await using var migrationContext = new AppDbContext(options, new NullCurrentCompanyContext(), new NullPublisher());
        await migrationContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await _azuriteContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting (not ConfigureAppConfiguration) because Program.cs reads this connection string
        // very early — before WebApplicationBuilder.Build() — and UseSetting is applied while the
        // host's configuration is still being assembled, so the value is visible in time.
        builder.UseSetting("ConnectionStrings:AssetManagementDb", _sqlContainer.GetConnectionString());
        builder.UseSetting("BlobStorage:ConnectionString", _azuriteContainer.GetConnectionString());
        // The whole suite shares one process/IP; F12's rate limiter would otherwise throttle bursty
        // tests that legitimately fire far more requests per second than any real user session does.
        builder.UseSetting("RateLimiting:PermitLimit", "1000000");

        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    private sealed class NullCurrentCompanyContext : ICurrentCompanyContext
    {
        public bool HasActiveCompany => false;

        public Guid? CompanyId => null;

        public IReadOnlyCollection<Guid> AccessibleCompanyIds => [];
    }

    /// <summary>This context only ever calls Database.MigrateAsync(), never SaveChangesAsync — the real
    /// app host (with a real IPublisher) handles every actual request in these tests.</summary>
    private sealed class NullPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not available on the migration-only context.");

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            throw new NotSupportedException("Not available on the migration-only context.");
    }
}
