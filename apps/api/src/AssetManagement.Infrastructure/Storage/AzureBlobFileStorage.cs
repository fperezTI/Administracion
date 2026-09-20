using AssetManagement.Application.Common.Interfaces;
using Azure.Storage.Blobs;

namespace AssetManagement.Infrastructure.Storage;

/// <summary>Azurite locally, a real Azure Storage account in production — same connection-string-per-
/// environment strategy as SQL Server (see F8 plan, decision 1). Single container for all companies;
/// isolation comes from the blob path prefix (<c>{companyId}/{entityType}/{entityId}/...</c>), not from
/// per-tenant containers — simpler to operate at the up-to-50-companies scale this system targets.</summary>
public sealed class AzureBlobFileStorage : IFileStorage
{
    private const string ContainerName = "documents";
    private readonly BlobContainerClient _container;

    public AzureBlobFileStorage(string connectionString)
    {
        // No eager container creation/connectivity check here — this runs at DI construction time
        // (effectively app startup), and Documents being unreachable must never crash the whole API for
        // every other module that has nothing to do with file storage.
        _container = new BlobContainerClient(connectionString, ContainerName);
    }

    public async Task UploadAsync(string blobPath, string contentType, Stream content, CancellationToken cancellationToken)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blob = _container.GetBlobClient(blobPath);
        await blob.UploadAsync(
            content, new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(string blobPath, CancellationToken cancellationToken)
    {
        var blob = _container.GetBlobClient(blobPath);
        return await blob.OpenReadAsync(cancellationToken: cancellationToken);
    }
}
