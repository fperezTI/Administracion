using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Infrastructure.Common;
using AssetManagement.Infrastructure.Configuration;
using AssetManagement.Infrastructure.DataRetention;
using AssetManagement.Infrastructure.Directory;
using AssetManagement.Infrastructure.Email;
using AssetManagement.Infrastructure.ImportExport;
using AssetManagement.Infrastructure.Persistence;
using AssetManagement.Infrastructure.Security;
using AssetManagement.Infrastructure.Storage;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        // The Data Protection key ring encrypts SystemSettings.GraphClientSecretCiphertext (see
        // ISecretProtector). It MUST be persisted somewhere durable in production — otherwise every secret
        // ever protected becomes permanently undecryptable the next time the App Service container
        // restarts (which happens routinely on redeploy). Reuses the same Storage account already used for
        // document blobs — only a new container. Locally (Azurite / no real storage configured) falls back
        // to ASP.NET Core's default local key storage: fine for dev, just means a re-entered secret after
        // the container is recreated.
        var dataProtectionBuilder = services.AddDataProtection().SetApplicationName("AssetManagement");
        if (!string.IsNullOrWhiteSpace(blobStorageConnectionString) && blobStorageConnectionString != "UseDevelopmentStorage=true")
        {
            var keysContainerClient = new BlobContainerClient(blobStorageConnectionString, "dataprotection-keys");
            keysContainerClient.CreateIfNotExists();
            dataProtectionBuilder.PersistKeysToAzureBlobStorage(keysContainerClient.GetBlobClient("keys.xml"));
        }

        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        services.AddScoped<ISystemSettingsProvider, SystemSettingsProvider>();

        // Pre-provisioning users from the tenant directory (see CreateUserFromDirectoryCommand) and
        // sending assignment notification emails (see AssignmentGroupSupport) both need the same app-only
        // Microsoft Graph credential — a separate concern from EntraId's token *validation* config above,
        // which needs no secret of its own. Both ConfigurableXxx classes below resolve the current
        // effective credentials (database override, or MicrosoftGraph:* configuration) via
        // ISystemSettingsProvider on every call, rather than once at startup, so a change made from the
        // system settings admin screen (Configuration.Update) takes effect immediately.
        services.AddHttpClient("GraphDirectory");
        services.AddScoped<IDirectoryUserSearch, ConfigurableDirectoryUserSearch>();

        services.AddHttpClient("GraphMail");
        services.AddScoped<IEmailSender, ConfigurableEmailSender>();

        var frontendBaseUrl = configuration["Frontend:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            services.AddSingleton<IFrontendLinkBuilder>(new FrontendLinkBuilder(frontendBaseUrl));
        }
        else
        {
            services.AddSingleton<IFrontendLinkBuilder>(new FrontendLinkBuilder("http://localhost:3000"));
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

        services.AddHostedService<ImportBatchBackgroundService>();
        // Configurable via DataRetention:NotificationRetentionDays/PurgeIntervalHours — see
        // docs/privacy-retention.md and ADR 0014.
        services.AddHostedService<NotificationRetentionBackgroundService>();

        return services;
    }
}
