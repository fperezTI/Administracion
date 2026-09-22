using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.UnitTests.TestSupport;

/// <summary>Reversible fake — real encryption is Infrastructure's concern (ASP.NET Core Data Protection),
/// this only needs to prove handlers never store/return the plaintext secret directly.</summary>
internal sealed class FakeSecretProtector : ISecretProtector
{
    private const string Prefix = "protected:";

    public string Protect(string plaintext) => Prefix + plaintext;

    public string? Unprotect(string ciphertext) =>
        ciphertext.StartsWith(Prefix, StringComparison.Ordinal) ? ciphertext[Prefix.Length..] : null;
}
