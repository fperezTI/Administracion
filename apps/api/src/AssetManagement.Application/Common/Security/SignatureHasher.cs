using System.Security.Cryptography;
using System.Text;

namespace AssetManagement.Application.Common.Security;

/// <summary>Computes the integrity hash stored on a <see cref="AssetManagement.Domain.Signature.SignatureRecord"/>
/// (pedido C6: "hash de integridad calculado sobre el contenido firmado"). Lives in Application, not
/// Domain, so the aggregate itself stays free of cryptography concerns — see ADR 0005.</summary>
public static class SignatureHasher
{
    public static string Hash(string canonicalPayload)
    {
        var bytes = Encoding.UTF8.GetBytes(canonicalPayload);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
