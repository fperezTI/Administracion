namespace AssetManagement.Application.Common.Interfaces;

/// <summary>
/// Decoupled work queue for <see cref="Domain.ImportExport.ImportBatch"/> processing (ADR 0003/0011):
/// <c>Channel&lt;T&gt;</c> + a <c>BackgroundService</c> locally/Docker Compose, Azure Storage Queue in
/// Azure — same contract either way, so <c>ImportBatchBackgroundService</c> never changes. Messages are
/// just the batch id; the worker is idempotent by inspecting the batch's own persisted
/// <see cref="Domain.ImportExport.ImportBatchStatus"/> rather than trusting anything about the message
/// itself (ADR 0003: "clave de idempotencia = ImportBatchId").
/// </summary>
public interface IImportQueue
{
    public Task EnqueueAsync(Guid importBatchId, CancellationToken cancellationToken);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}
