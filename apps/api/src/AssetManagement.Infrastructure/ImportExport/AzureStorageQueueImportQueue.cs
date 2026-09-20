using System.Runtime.CompilerServices;
using AssetManagement.Application.Common.Interfaces;
using Azure.Storage.Queues;

namespace AssetManagement.Infrastructure.ImportExport;

/// <summary>Azure Storage Queue implementation of <see cref="IImportQueue"/> for real Azure deployments
/// (ADR 0003/0011) — same contract as <see cref="ChannelImportQueue"/>, so
/// <c>ImportBatchBackgroundService</c> never changes. Deletes a message as soon as it's received rather
/// than after processing completes: same "not durable across a crash mid-message" simplification V1
/// already accepts for the local queue, and the worker is idempotent on the batch's own persisted status
/// regardless (ADR 0003 — "clave de idempotencia = ImportBatchId").</summary>
public sealed class AzureStorageQueueImportQueue : IImportQueue
{
    private const string QueueName = "import-batches";
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan VisibilityTimeout = TimeSpan.FromMinutes(5);

    private readonly QueueClient _queueClient;

    public AzureStorageQueueImportQueue(string connectionString)
    {
        _queueClient = new QueueClient(connectionString, QueueName);
        _queueClient.CreateIfNotExists();
    }

    public async Task EnqueueAsync(Guid importBatchId, CancellationToken cancellationToken) =>
        await _queueClient.SendMessageAsync(importBatchId.ToString(), cancellationToken);

    public async IAsyncEnumerable<Guid> DequeueAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var response = await _queueClient.ReceiveMessagesAsync(maxMessages: 10, VisibilityTimeout, cancellationToken);

            if (response.Value.Length == 0)
            {
                await Task.Delay(PollInterval, cancellationToken);
                continue;
            }

            foreach (var message in response.Value)
            {
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);

                if (Guid.TryParse(message.MessageText, out var importBatchId))
                {
                    yield return importBatchId;
                }
            }
        }
    }
}
