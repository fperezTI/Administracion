using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.UnitTests.TestSupport;

/// <summary>In-memory stand-in for handler tests that only need *some* readable blob back — the real
/// Azure Blob Storage/Azurite behavior is covered by integration tests (same rationale as
/// FakeFolioGenerator), never faked there.</summary>
internal sealed class FakeFileStorage : IFileStorage
{
    private readonly Dictionary<string, byte[]> _blobs = new();

    public Task UploadAsync(string blobPath, string contentType, Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        _blobs[blobPath] = buffer.ToArray();
        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadAsync(string blobPath, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream(_blobs[blobPath]));
}
