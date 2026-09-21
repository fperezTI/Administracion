namespace AssetManagement.Application.Common.Interfaces;

/// <summary>A person found in the tenant's Microsoft Entra ID directory — not yet (necessarily) a local
/// <see cref="AssetManagement.Domain.Identity.User"/>. Used to pre-provision someone before their first
/// login, see <see cref="AssetManagement.Application.Identity.Users.CreateUserFromDirectoryCommand"/>.</summary>
public sealed record DirectoryUser(Guid EntraObjectId, string DisplayName, string Email);

/// <summary>Looks people up in the tenant directory (Microsoft Graph) so an administrator can add someone
/// before they ever sign in — normally a <see cref="AssetManagement.Domain.Identity.User"/> is only
/// created on first login (JIT provisioning, see docs/security/authentication.md). The implementation
/// used depends on whether Graph application credentials are configured — see
/// AssetManagement.Infrastructure.DependencyInjection.</summary>
public interface IDirectoryUserSearch
{
    Task<IReadOnlyList<DirectoryUser>> SearchAsync(string query, CancellationToken cancellationToken);
}
