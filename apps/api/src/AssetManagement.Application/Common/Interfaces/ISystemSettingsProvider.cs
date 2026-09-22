namespace AssetManagement.Application.Common.Interfaces;

/// <summary>Microsoft Graph settings actually in effect right now — the SystemSettings database row
/// (see UpdateSystemSettingsCommand) overriding appsettings/environment configuration field by field,
/// resolved fresh on every call so a change made from the admin screen takes effect immediately, without
/// a redeploy or restart.</summary>
public sealed record EffectiveGraphSettings(string? TenantId, string? ClientId, string? ClientSecret, string? SenderMailbox);

public interface ISystemSettingsProvider
{
    Task<EffectiveGraphSettings> GetGraphSettingsAsync(CancellationToken cancellationToken);
}
