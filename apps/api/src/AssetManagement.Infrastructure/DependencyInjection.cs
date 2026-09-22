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

        // Pre-provisioning users from the tenant directory (see CreateUserFromDirectoryCommand) and
        // sending assignment notification emails (see AssignmentGroupSupport) both need the same app-only
        // Microsoft Graph token — a separate concern from EntraId's token *validation* config above, which
        // needs no secret of its own. Registered once here so both features share one credential/HttpClient.
        var graphTenantId = configuration["MicrosoftGraph:TenantId"];
        var graphClientId = configuration["MicrosoftGraph:ClientId"];
        var graphClientSecret = configuration["MicrosoftGraph:ClientSecret"];
        var graphSenderMailbox = configuration["MicrosoftGraph:SenderMailbox"];
        var graphCredentialConfigured = !string.IsNullOrWhiteSpace(graphTenantId) && !string.IsNullOrWhiteSpace(graphClientId)
            && !string.IsNullOrWhiteSpace(graphClientSecret);
        if (graphCredentialConfigured)
        {
            services.AddSingleton(new ClientSecretCredential(graphTenantId, graphClientId, graphClientSecret));
            services.AddHttpClient<IDirectoryUserSearch, GraphDirectoryUserSearch>();
        }
        else
        {
            services.AddScoped<IDirectoryUserSearch, UnconfiguredDirectoryUserSearch>();
        }

        var smtpHost = configuration["Smtp:Host"];
        if (!string.IsNullOrWhiteSpace(smtpHost))
        {
            var smtpPort = configuration.GetValue("Smtp:Port", 25);
            var smtpUsername = configuration["Smtp:Username"];
            var smtpPassword = configuration["Smtp:Password"];
            var smtpFromAddress = configuration["Smtp:FromAddress"] ?? "no-reply@example.com";
            services.AddSingleton<IEmailSender>(sp => new SmtpEmailSender(
                smtpHost, smtpPort, smtpUsername, smtpPassword, smtpFromAddress,
                sp.GetRequiredService<ILogger<SmtpEmailSender>>()));
        }
        else if (graphCredentialConfigured && !string.IsNullOrWhiteSpace(graphSenderMailbox))
        {
            services.AddHttpClient("GraphMail");
            services.AddSingleton<IEmailSender>(sp => new GraphEmailSender(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("GraphMail"),
                sp.GetRequiredService<ClientSecretCredential>(),
                graphSenderMailbox,
                sp.GetRequiredService<ILogger<GraphEmailSender>>()));
        }
        else
        {
            services.AddSingleton<IEmailSender, NoOpEmailSender>();
        }

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
