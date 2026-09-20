using AssetManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AssetManagement.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so `dotnet ef migrations add/update` works without spinning up the full DI host.
/// The connection here is only used to generate migrations; it is never used at runtime.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // "migrations add" only needs a syntactically valid connection string to build the model in
        // memory — it never opens a real connection. The local Docker Compose SQL Server password is read
        // from the environment so no credential-shaped value is ever hardcoded here.
        var password = Environment.GetEnvironmentVariable("SQLSERVER_LOCAL_DEV_PASSWORD") ?? "not-a-real-secret";
        var connectionString =
            $"Server=localhost,1433;Database=AssetManagement;User Id=sa;Password={password};TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options, new DesignTimeCurrentCompanyContext(), new DesignTimePublisher());
    }

    /// <summary>Never queried at design time — HasQueryFilter only needs a real field reference to
    /// build its expression tree, not a meaningful value (see AppDbContext.OnModelCreating).</summary>
    private sealed class DesignTimeCurrentCompanyContext : ICurrentCompanyContext
    {
        public bool HasActiveCompany => false;

        public Guid? CompanyId => null;

        public IReadOnlyCollection<Guid> AccessibleCompanyIds => [];
    }

    /// <summary>Never invoked at design time — `migrations add`/`database update` never call
    /// SaveChangesAsync (see AppDbContext's domain event dispatch, F4), so this only needs to satisfy
    /// the constructor.</summary>
    private sealed class DesignTimePublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not available at design time.");

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            throw new NotSupportedException("Not available at design time.");
    }
}
