using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Configuration;

/// <summary>
/// A single, application-wide settings row (never one per company — there is one Entra ID tenant and one
/// Graph app registration for the whole app, see docs/multi-company.md). Fields left null/empty defer to
/// appsettings/environment configuration (see Infrastructure's SystemSettingsProvider) so an admin can
/// override just one value from the UI without having to re-enter everything already configured via
/// Key Vault/App Settings.
/// </summary>
public sealed class SystemSettings : AggregateRoot<Guid>
{
    /// <summary>Fixed id — this table only ever has one row.</summary>
    public static readonly Guid SingletonId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public string? SenderMailbox { get; private set; }
    public string? GraphTenantId { get; private set; }
    public string? GraphClientId { get; private set; }

    /// <summary>Never plaintext — protected via Infrastructure's ISecretProtector before it ever reaches
    /// this property.</summary>
    public string? GraphClientSecretCiphertext { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private SystemSettings()
    {
    }

    private SystemSettings(Guid id, DateTimeOffset nowUtc)
        : base(id)
    {
        UpdatedAtUtc = nowUtc;
    }

    public static SystemSettings CreateEmpty(DateTimeOffset nowUtc) => new(SingletonId, nowUtc);

    public void Update(
        string? senderMailbox,
        string? graphTenantId,
        string? graphClientId,
        string? graphClientSecretCiphertext,
        Guid? updatedByUserId,
        DateTimeOffset nowUtc)
    {
        Validate(senderMailbox, graphTenantId, graphClientId);

        SenderMailbox = NormalizeOrNull(senderMailbox);
        GraphTenantId = NormalizeOrNull(graphTenantId);
        GraphClientId = NormalizeOrNull(graphClientId);
        GraphClientSecretCiphertext = NormalizeOrNull(graphClientSecretCiphertext);
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = nowUtc;
    }

    private static string? NormalizeOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Validate(string? senderMailbox, string? graphTenantId, string? graphClientId)
    {
        if (!string.IsNullOrWhiteSpace(senderMailbox)
            && (!senderMailbox.Contains('@') || senderMailbox.Any(char.IsWhiteSpace)))
        {
            throw new DomainException("El buzón remitente no tiene el formato de un correo válido.");
        }

        if (!string.IsNullOrWhiteSpace(graphTenantId) && !Guid.TryParse(graphTenantId, out _))
        {
            throw new DomainException("El Tenant ID de Microsoft Graph debe ser un GUID válido.");
        }

        if (!string.IsNullOrWhiteSpace(graphClientId) && !Guid.TryParse(graphClientId, out _))
        {
            throw new DomainException("El Client ID de Microsoft Graph debe ser un GUID válido.");
        }
    }
}
