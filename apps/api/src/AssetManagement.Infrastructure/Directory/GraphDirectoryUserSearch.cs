using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using Azure.Core;
using Azure.Identity;

namespace AssetManagement.Infrastructure.Directory;

/// <summary>
/// Looks people up in the tenant's Microsoft Entra ID directory via the Graph REST API directly (v1.0),
/// rather than the `Microsoft.Graph` SDK — deliberately: the SDK is a large, Kiota-generated surface that's
/// easy to get subtly wrong without being able to check its exact version-specific shape, while this is a
/// single well-known REST call. <see cref="ClientSecretCredential"/> (Azure.Identity, already a stable,
/// long-standing API) handles acquiring and caching the app-only token internally — no manual token cache
/// needed here.
/// </summary>
public sealed class GraphDirectoryUserSearch(HttpClient httpClient, ClientSecretCredential credential) : IDirectoryUserSearch
{
    private static readonly string[] GraphScopes = ["https://graph.microsoft.com/.default"];

    public async Task<IReadOnlyList<DirectoryUser>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var token = await credential.GetTokenAsync(new TokenRequestContext(GraphScopes), cancellationToken);

        var escaped = EscapeODataLiteral(query);
        var filter = $"startswith(displayName,'{escaped}') or startswith(mail,'{escaped}') or startswith(userPrincipalName,'{escaped}')";
        var url = "https://graph.microsoft.com/v1.0/users" +
            $"?$filter={Uri.EscapeDataString(filter)}&$select=id,displayName,mail,userPrincipalName&$top=25";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
        request.Headers.Add("ConsistencyLevel", "eventual");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ConflictException(
                "No fue posible consultar el directorio de Entra ID — revisa los permisos de Microsoft Graph.");
        }

        var payload = await response.Content.ReadFromJsonAsync<GraphUsersResponse>(cancellationToken);
        return (payload?.Value ?? [])
            .Where(u => !string.IsNullOrWhiteSpace(u.Id) && !string.IsNullOrWhiteSpace(u.DisplayName))
            .Select(u => new DirectoryUser(Guid.Parse(u.Id!), u.DisplayName!, u.Mail ?? u.UserPrincipalName ?? ""))
            .ToList();
    }

    /// <summary>OData string literals escape a single quote by doubling it — the same injection class as
    /// unescaped SQL string concatenation if skipped, since <paramref name="value"/> is free-text search
    /// input interpolated directly into the `$filter` expression.</summary>
    private static string EscapeODataLiteral(string value) => value.Replace("'", "''");

    private sealed class GraphUsersResponse
    {
        [JsonPropertyName("value")]
        public List<GraphUser>? Value { get; set; }
    }

    private sealed class GraphUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("mail")]
        public string? Mail { get; set; }

        [JsonPropertyName("userPrincipalName")]
        public string? UserPrincipalName { get; set; }
    }
}
