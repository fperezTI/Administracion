using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.Infrastructure.Directory;

/// <summary>Registered when <c>MicrosoftGraph:TenantId/ClientId/ClientSecret</c> aren't configured (see
/// DependencyInjection.cs) — unlike <c>NoOpEmailSender</c>, silently returning nothing here would hide a
/// misconfiguration behind an always-empty search result, so this throws a clear, actionable error
/// instead.</summary>
public sealed class UnconfiguredDirectoryUserSearch : IDirectoryUserSearch
{
    public Task<IReadOnlyList<DirectoryUser>> SearchAsync(string query, CancellationToken cancellationToken) =>
        throw new ConflictException(
            "La búsqueda del directorio de Entra ID no está configurada. Ver docs/security/entra-id-setup.md.");
}
