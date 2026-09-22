using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using MediatR;

namespace AssetManagement.Application.SystemConfiguration;

/// <summary>Never includes the Graph client secret itself — only whether one is configured — see
/// UpdateSystemSettingsCommand for why.</summary>
public sealed record SystemSettingsDto(
    string SenderMailbox, string GraphTenantId, string GraphClientId, bool HasGraphClientSecretConfigured);

public sealed record GetSystemSettingsQuery : IRequest<SystemSettingsDto>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Configuration.Read;
}

/// <summary>Reuses ISystemSettingsProvider — the same port GraphEmailSender/GraphDirectoryUserSearch use
/// internally — so the admin screen shows the values actually in effect (database override, or the
/// appsettings/Key Vault fallback), not just what happens to be saved in the database.</summary>
public sealed class GetSystemSettingsQueryHandler(ISystemSettingsProvider settingsProvider)
    : IRequestHandler<GetSystemSettingsQuery, SystemSettingsDto>
{
    public async Task<SystemSettingsDto> Handle(GetSystemSettingsQuery request, CancellationToken cancellationToken)
    {
        var effective = await settingsProvider.GetGraphSettingsAsync(cancellationToken);

        return new SystemSettingsDto(
            effective.SenderMailbox ?? "",
            effective.TenantId ?? "",
            effective.ClientId ?? "",
            !string.IsNullOrEmpty(effective.ClientSecret));
    }
}
