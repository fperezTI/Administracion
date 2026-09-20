namespace AssetManagement.Application.Common.Interfaces;

/// <summary>
/// Identity of the authenticated caller, resolved by Infrastructure from the validated Entra ID token.
/// Never trust a caller-supplied user id; this is populated from the token claims only.
/// </summary>
public interface ICurrentUserContext
{
    public bool IsAuthenticated { get; }

    public Guid? UserId { get; }

    public Guid? EntraObjectId { get; }

    public string? DisplayName { get; }

    public string? Email { get; }

    /// <summary>Caller's IP address, for e-signature metadata (pedido C6/§18) — never used for access
    /// control, only recorded alongside a <see cref="Domain.Signature.SignatureRecord"/>.</summary>
    public string? IpAddress { get; }

    /// <summary>Caller's User-Agent header, for e-signature metadata — same caveat as <see cref="IpAddress"/>.</summary>
    public string? UserAgent { get; }

    /// <summary>Per-request correlation id (pedido §11), for <c>AuditBehavior</c> (F8) — the same id a
    /// technical Serilog log line for this request would carry, letting an admin cross-reference the two
    /// without mixing functional audit and technical logs into one store.</summary>
    public string? CorrelationId { get; }
}
