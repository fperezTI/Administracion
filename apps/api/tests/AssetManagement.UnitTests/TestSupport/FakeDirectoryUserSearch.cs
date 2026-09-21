using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.UnitTests.TestSupport;

/// <summary>Canned results for handler tests — the real Microsoft Graph call is a thin IO adapter
/// (GraphDirectoryUserSearch), same rationale as FakeFileStorage/FakeFolioGenerator, never faked there.</summary>
internal sealed class FakeDirectoryUserSearch : IDirectoryUserSearch
{
    public IReadOnlyList<DirectoryUser> Results { get; set; } = [];

    public Task<IReadOnlyList<DirectoryUser>> SearchAsync(string query, CancellationToken cancellationToken) =>
        Task.FromResult(Results);
}
