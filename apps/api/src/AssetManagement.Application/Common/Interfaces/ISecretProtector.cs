namespace AssetManagement.Application.Common.Interfaces;

/// <summary>
/// Encrypts values at rest before they leave Application (e.g. a real Microsoft Graph client secret
/// stored in SystemSettings — see docs behind UpdateSystemSettingsCommand). Implemented in Infrastructure
/// via ASP.NET Core's Data Protection API.
/// </summary>
public interface ISecretProtector
{
    string Protect(string plaintext);

    /// <summary>Never throws — returns null if the ciphertext can't be decrypted (e.g. the encryption key
    /// changed), so callers can treat it as "not configured" instead of crashing.</summary>
    string? Unprotect(string ciphertext);
}
