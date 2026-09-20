namespace AssetManagement.Application.Common.Interfaces;

/// <summary>Blob storage port (pedido: Documentos/evidencias) — Azurite locally, a real Azure Storage
/// account in production, same connection-string-per-environment strategy as SQL Server.</summary>
public interface IFileStorage
{
    public Task UploadAsync(string blobPath, string contentType, Stream content, CancellationToken cancellationToken);

    public Task<Stream> OpenReadAsync(string blobPath, CancellationToken cancellationToken);
}
