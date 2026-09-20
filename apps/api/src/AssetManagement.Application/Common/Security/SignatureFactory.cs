using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Domain.Signature;

namespace AssetManagement.Application.Common.Security;

/// <summary>
/// Builds a <see cref="SignatureRecord"/> for either mechanism F4 supports, shared by every command that
/// lets the caller choose how to sign (approval decisions today; F3's assignment flows predate this and
/// keep calling <see cref="SignatureRecord.Create"/> directly with a fixed mechanism). For
/// <see cref="SignatureRecord.TypedConfirmationMechanism"/> the signer types their name (a deliberate act,
/// same as F3); for <see cref="SignatureRecord.DrawnSignatureMechanism"/> the signer's own account name is
/// used automatically (typing it again would be redundant with drawing) and the image is required instead.
/// </summary>
public static class SignatureFactory
{
    public static SignatureRecord Create(
        Guid companyId, string contextType, Guid contextId, Guid signerUserId, string signerAccountDisplayName,
        string mechanism, string? typedFullName, string? signatureImageDataUrl, string? ipAddress,
        string? userAgent, DateTimeOffset nowUtc)
    {
        string signerDisplayName;
        if (mechanism == SignatureRecord.TypedConfirmationMechanism)
        {
            if (string.IsNullOrWhiteSpace(typedFullName))
            {
                throw new ConflictException("Escribe tu nombre completo para firmar con este mecanismo.");
            }

            signerDisplayName = typedFullName.Trim();
        }
        else if (mechanism == SignatureRecord.DrawnSignatureMechanism)
        {
            if (string.IsNullOrWhiteSpace(signatureImageDataUrl))
            {
                throw new ConflictException("Dibuja tu firma para firmar con este mecanismo.");
            }

            signerDisplayName = signerAccountDisplayName;
        }
        else
        {
            throw new ConflictException($"Mecanismo de firma no reconocido: '{mechanism}'.");
        }

        var payload = $"{contextType}|{contextId}|{signerUserId}|{mechanism}|{nowUtc:O}";
        return SignatureRecord.Create(
            companyId, contextType, contextId, signerUserId, signerDisplayName, ipAddress, userAgent,
            SignatureHasher.Hash(payload), mechanism,
            mechanism == SignatureRecord.DrawnSignatureMechanism ? signatureImageDataUrl : null, nowUtc, signerUserId);
    }
}
