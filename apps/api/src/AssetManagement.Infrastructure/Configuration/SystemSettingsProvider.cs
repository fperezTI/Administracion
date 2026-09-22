using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AssetManagement.Infrastructure.Configuration;

/// <summary>Reads the SystemSettings singleton row fresh from the database on every call (no caching —
/// see DependencyInjection.cs's comment on why) and falls back, field by field, to
/// MicrosoftGraph:* configuration for whatever isn't overridden there.</summary>
public sealed class SystemSettingsProvider(IApplicationDbContext db, ISecretProtector secretProtector, IConfiguration configuration)
    : ISystemSettingsProvider
{
    public async Task<EffectiveGraphSettings> GetGraphSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings
            .FirstOrDefaultAsync(s => s.Id == SystemSettings.SingletonId, cancellationToken);

        var tenantId = FirstNonEmpty(settings?.GraphTenantId, configuration["MicrosoftGraph:TenantId"]);
        var clientId = FirstNonEmpty(settings?.GraphClientId, configuration["MicrosoftGraph:ClientId"]);
        var senderMailbox = FirstNonEmpty(settings?.SenderMailbox, configuration["MicrosoftGraph:SenderMailbox"]);

        var clientSecret = string.IsNullOrWhiteSpace(settings?.GraphClientSecretCiphertext)
            ? configuration["MicrosoftGraph:ClientSecret"]
            : secretProtector.Unprotect(settings.GraphClientSecretCiphertext);

        return new EffectiveGraphSettings(tenantId, clientId, clientSecret, senderMailbox);
    }

    private static string? FirstNonEmpty(string? primary, string? fallback) =>
        string.IsNullOrWhiteSpace(primary) ? fallback : primary;
}
