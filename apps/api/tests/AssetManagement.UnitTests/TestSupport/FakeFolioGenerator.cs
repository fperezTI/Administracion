using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.UnitTests.TestSupport;

/// <summary>EfFolioGenerator needs a real SQL Server (it issues a MERGE statement not supported by the
/// EF Core InMemory provider) — its atomic-increment behavior is covered by integration tests instead.
/// This fake is for handler tests that only need *some* deterministic folio.</summary>
internal sealed class FakeFolioGenerator : IFolioGenerator
{
    private int _counter;

    public Task<string> NextAsync(Guid companyId, string documentType, CancellationToken cancellationToken)
    {
        _counter += 1;
        return Task.FromResult($"{documentType}-{_counter:D6}");
    }
}
