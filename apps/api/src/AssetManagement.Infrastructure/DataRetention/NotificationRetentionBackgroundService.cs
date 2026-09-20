using AssetManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.DataRetention;

/// <summary>
/// The one real automated purge of V1 (pedido: "retención configurable", F12 — ver
/// docs/privacy-retention.md). <c>Notification</c> was already documented as "efímera por diseño" in F8 —
/// the safe, uncontroversial candidate to actually delete on a schedule, unlike <c>Movement</c>/
/// <c>AuditEntry</c> (the legal record of custody/actions, kept indefinitely in V1, never purged here).
/// Same hosting pattern as <c>ImportBatchBackgroundService</c> (F9): a singleton service resolving a
/// scoped <see cref="IApplicationDbContext"/> per run via <see cref="IServiceScopeFactory"/>, since
/// <c>BackgroundService</c> itself outlives any single request scope.
/// </summary>
public sealed class NotificationRetentionBackgroundService(
    IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<NotificationRetentionBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retentionDays = configuration.GetValue("DataRetention:NotificationRetentionDays", 90);
        var intervalHours = configuration.GetValue("DataRetention:PurgeIntervalHours", 24);
        using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));

        do
        {
            try
            {
                await PurgeAsync(retentionDays, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error purgando notificaciones por retención.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>Exposed (not private) so integration tests can exercise a single purge deterministically
    /// without running the full timer loop.</summary>
    public async Task PurgeAsync(int retentionDays, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var threshold = clock.UtcNow.AddDays(-retentionDays);
        var deleted = await db.Notifications.Where(n => n.CreatedAtUtc < threshold).ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            logger.LogInformation(
                "Purgadas {Count} notificaciones con más de {RetentionDays} días (retención configurada).", deleted, retentionDays);
        }
    }
}
