using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Infrastructure.Common;
using AssetManagement.Infrastructure.DataRetention;
using AssetManagement.Infrastructure.Directory;
using AssetManagement.Infrastructure.Email;
using AssetManagement.Infrastructure.ImportExport;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.Infrastructure.Security;
using AssetManagement.Infrastructure.Storage;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AssetManagementDb")
            ?? throw new InvalidOperationException(
                "Missing connection string 'AssetManagementDb'. Set it via appsettings, user-secrets, or the " +
                "ConnectionStrings__AssetManagementDb environment variable (see docker-compose.yml).");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IHostEnvironmentInfo, HostEnvironmentInfo>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, HttpContextCurrentUserContext>();
        services.AddScoped<ICurrentCompanyContext, HttpContextCurrentCompanyContext>();
        services.AddScoped<IPermissionChecker, EfPermissionChecker>();
        services.AddScoped<IFolioGenerator, EfFolioGenerator>();

        // Azurite's well-known local connection string (public, documented on Microsoft Learn — not a
        // real secret) is the default so local `dotnet run`/tests work without extra setup when Azurite
        // is reachable; production sets BlobStorage__ConnectionString to a real Storage account.
        var blobStorageConnectionString = configuration["BlobStorage:ConnectionString"] ?? "UseDevelopmentStorage=true";
        services.AddSingleton<IFileStorage>(_ => new AzureBlobFileStorage(blobStorageConnectionString));

        var smtpHost = configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            services.AddSingleton<IEmailSender, NoOpEmailSender>();
        }
        else
        {
            var smtpPort = configuration.GetValue("Smtp:Port", 25);
            var smtpUsername = configuration["Smtp:Username"];
            var smtpPassword = configuration["Smtp:Password"];
            var smtpFromAddress = configuration["Smtp:FromAddress"] ?? "no-reply@example.com";
            services.AddSingleton<IEmailSender>(sp => new SmtpEmailSender(
                smtpHost, smtpPort, smtpUsername, smtpPassword, smtpFromAddress,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SmtpEmailSender>>()));
        }

        // ADR 0003/0011: Azure Storage Queue when a real connection string is configured (production),
        // the in-process Channel<T> queue otherwise (local/Docker Compose) — same IImportQueue contract
        // either way, ImportBatchBackgroundService never changes.
        var importQueueConnectionString = configuration["ImportQueue:AzureStorageQueue:ConnectionString"];
        if (string.IsNullOrWhiteSpace(importQueueConnectionString))
        {
            services.AddSingleton<IImportQueue, ChannelImportQueue>();
        }
        else
        {
            services.AddSingleton<IImportQueue>(_ => new AzureStorageQueueImportQueue(importQueueConnectionString));
        }

        // Pre-provisioning users from the tenant directory (see CreateUserFromDirectoryCommand) needs an
        // app-only Microsoft Graph token — a separate concern from EntraId's token *validation* config
        // above, which needs no secret of its own. Falls back to a search that fails loudly, rather than
        // silently returning nothing, when Graph app credentials aren't configured yet (see
        // docs/security/entra-id-setup.md).
        var graphTenantId = configuration["MicrosoftGraph:TenantId"];
        var graphClientId = configuration["MicrosoftGraph:ClientId"];
        var graphClientSecret = configuration["MicrosoftGraph:ClientSecret"];
        if (!string.IsNullOrWhiteSpace(graphTenantId) && !string.IsNullOrWhiteSpace(graphClientId)
            && !string.IsNullOrWhiteSpace(graphClientSecret))
        {
            services.AddSingleton(new ClientSecretCredential(graphTenantId, graphClientId, graphClientSecret));
            services.AddHttpClient<IDirectoryUserSearch, GraphDirectoryUserSearch>();
        }
        else
        {
            services.AddScoped<IDirectoryUserSearch, UnconfiguredDirectoryUserSearch>();
        }

        services.AddHostedService<ImportBatchBackgroundService>();
        // Configurable via DataRetention:NotificationRetentionDays/PurgeIntervalHours — see
        // docs/privacy-retention.md and ADR 0014.
        services.AddHostedService<NotificationRetentionBackgroundService>();

        return services;
    }
}
