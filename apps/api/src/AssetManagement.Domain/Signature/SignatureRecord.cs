using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Signature;

/// <summary>
/// A simple electronic signature (pedido C6/§18: "registra IP/dispositivo/hash") capturing enough to
/// prove who confirmed what and when. F3 shipped a single mechanism (<see cref="TypedConfirmationMechanism"/>);
/// F4 adds <see cref="DrawnSignatureMechanism"/> without changing this shape (ADR 0005/0006) — the caller
/// picks the mechanism, this aggregate just enforces that exactly the matching payload is present.
/// Immutable once created: there is no update or delete, only <see cref="Create"/>. <see cref="ContentHash"/>
/// is computed by the Application handler over a canonical payload of what was signed, not by the domain,
/// to keep this aggregate free of cryptography concerns.
/// </summary>
public sealed class SignatureRecord : AuditableAggregateRoot<Guid>
{
    public const string TypedConfirmationMechanism = "TypedConfirmation";
    public const string DrawnSignatureMechanism = "DrawnSignature";

    public Guid CompanyId { get; private set; }
    public string ContextType { get; private set; } = null!;
    public Guid ContextId { get; private set; }
    public Guid SignerUserId { get; private set; }
    public string SignerDisplayName { get; private set; } = null!;
    public DateTimeOffset SignedAtUtc { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string ContentHash { get; private set; } = null!;
    public string Mechanism { get; private set; } = null!;
    public string? SignatureImageDataUrl { get; private set; }

    private SignatureRecord()
    {
    }

    private SignatureRecord(
        Guid id, Guid companyId, string contextType, Guid contextId, Guid signerUserId,
        string signerDisplayName, string? ipAddress, string? userAgent, string contentHash, string mechanism,
        string? signatureImageDataUrl, DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        CompanyId = companyId;
        ContextType = contextType;
        ContextId = contextId;
        SignerUserId = signerUserId;
        SignerDisplayName = signerDisplayName;
        SignedAtUtc = nowUtc;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        ContentHash = contentHash;
        Mechanism = mechanism;
        SignatureImageDataUrl = signatureImageDataUrl;
    }

    public static SignatureRecord Create(
        Guid companyId, string contextType, Guid contextId, Guid signerUserId, string signerDisplayName,
        string? ipAddress, string? userAgent, string contentHash, string mechanism, string? signatureImageDataUrl,
        DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(contextType))
        {
            throw new DomainException("El tipo de contexto de la firma es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(signerDisplayName))
        {
            throw new DomainException("El nombre de quien firma es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(contentHash))
        {
            throw new DomainException("El hash de contenido de la firma es obligatorio.");
        }

        if (mechanism != TypedConfirmationMechanism && mechanism != DrawnSignatureMechanism)
        {
            throw new DomainException($"Mecanismo de firma no reconocido: '{mechanism}'.");
        }

        var hasImage = !string.IsNullOrWhiteSpace(signatureImageDataUrl);
        if (mechanism == DrawnSignatureMechanism && !hasImage)
        {
            throw new DomainException("La firma dibujada requiere la imagen capturada.");
        }

        if (mechanism == TypedConfirmationMechanism && hasImage)
        {
            throw new DomainException("La confirmación escrita no lleva imagen de firma.");
        }

        return new SignatureRecord(
            Guid.NewGuid(), companyId, contextType, contextId, signerUserId, signerDisplayName.Trim(),
            ipAddress, userAgent, contentHash, mechanism, signatureImageDataUrl, nowUtc, createdByUserId);
    }
}
