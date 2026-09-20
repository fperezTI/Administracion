using System.Threading.Channels;
using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.Infrastructure.ImportExport;

/// <summary>Local/Docker Compose implementation of <see cref="IImportQueue"/> (ADR 0003/0011) —
/// registered as a singleton so the same channel is shared between HTTP-scoped enqueues and the
/// singleton <c>ImportBatchBackgroundService</c>'s dequeue loop. In-memory only, same durability the
/// Channel-based worker already accepted for V1: a restart loses anything not yet picked up (nothing
/// dead-letters or persists mid-queue) — acceptable because the worker is idempotent on the batch's own
/// persisted status, not on the message itself.</summary>
public sealed class ChannelImportQueue : IImportQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public async Task EnqueueAsync(Guid importBatchId, CancellationToken cancellationToken) =>
        await _channel.Writer.WriteAsync(importBatchId, cancellationToken);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
