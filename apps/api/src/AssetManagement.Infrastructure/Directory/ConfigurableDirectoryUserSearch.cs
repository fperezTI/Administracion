using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.Infrastructure.Directory;

/// <summary>The only IDirectoryUserSearch registered in DI — resolves Graph credentials fresh on every
/// search via SystemSettingsProvider so a change made from the system settings admin screen takes effect
/// immediately, instead of only after a redeploy.</summary>
public sealed class ConfigurableDirectoryUserSearch(ISystemSettingsProvider settingsProvider, IHttpClientFactory httpClientFactory)
    : IDirectoryUserSearch
{
    public async Task<IReadOnlyList<DirectoryUser>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var graph = await settingsProvider.GetGraphSettingsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(graph.TenantId) || string.IsNullOrWhiteSpace(graph.ClientId)
            || string.IsNullOrWhiteSpace(graph.ClientSecret))
        {
            return await new UnconfiguredDirectoryUserSearch().SearchAsync(query, cancellationToken);
        }

        var search = new GraphDirectoryUserSearch(
            httpClientFactory.CreateClient("GraphDirectory"), graph.TenantId, graph.ClientId, graph.ClientSecret);
        return await search.SearchAsync(query, cancellationToken);
    }
}
