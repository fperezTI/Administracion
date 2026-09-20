using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.ImportExport;
using AssetManagement.Domain.ImportExport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.ImportExport;

/// <summary>
/// The single worker behind <see cref="IImportQueue"/> (ADR 0003/0011) — always registered regardless of
/// which <see cref="IImportQueue"/> implementation backs it, so local/Docker Compose and a real Azure
/// deployment share the exact same dispatch logic. Idempotent: it never trusts what the message claims,
/// only the batch's own persisted <see cref="ImportBatchStatus"/> — <c>Queued</c> means "run validation",
/// <c>Processing</c> means "run the commit", anything else (already terminal, or a stale re-delivery) is
/// a no-op.
/// </summary>
public sealed class ImportBatchBackgroundService(
    IImportQueue importQueue, IServiceScopeFactory scopeFactory, ILogger<ImportBatchBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var importBatchId in importQueue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var processor = scope.ServiceProvider.GetRequiredService<ImportBatchProcessor>();

                var status = await db.ImportBatches.IgnoreQueryFilters().AsNoTracking()
                    .Where(b => b.Id == importBatchId)
                    .Select(b => (ImportBatchStatus?)b.Status)
                    .FirstOrDefaultAsync(stoppingToken);

                switch (status)
                {
                    case ImportBatchStatus.Queued:
                        await processor.ValidateAsync(importBatchId, stoppingToken);
                        break;
                    case ImportBatchStatus.Processing:
                        await processor.CommitAsync(importBatchId, stoppingToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error procesando el lote de importación {ImportBatchId}.", importBatchId);
            }
        }
    }
}
