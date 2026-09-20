using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.UnitTests.TestSupport;

/// <summary>Builds a real AppDbContext (same entity configurations, seed data and query filters as
/// production) against a fresh EF Core InMemory database per test — used for Application handler
/// tests that need genuine query/persistence behavior without a SQL Server container.</summary>
internal static class InMemoryAppDbContextFactory
{
    public static AppDbContext Create(ICurrentCompanyContext? companyContext = null, string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, companyContext ?? new FakeCurrentCompanyContext(), new FakePublisher());
    }
}
