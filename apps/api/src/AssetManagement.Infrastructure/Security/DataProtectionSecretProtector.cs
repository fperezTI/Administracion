using System.Security.Cryptography;
using AssetManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace AssetManagement.Infrastructure.Security;

/// <summary>ASP.NET Core's own Data Protection API — no third-party crypto library needed. The key ring
/// must be persisted somewhere durable (see DependencyInjection.cs's AddDataProtection call) or every
/// secret ever protected here becomes permanently undecryptable the next time the container restarts.</summary>
public sealed class DataProtectionSecretProtector : ISecretProtector
{
    private const string Purpose = "AssetManagement.SystemSettings.v1";

    private readonly IDataProtector _protector;
    private readonly ILogger<DataProtectionSecretProtector> _logger;

    public DataProtectionSecretProtector(IDataProtectionProvider provider, ILogger<DataProtectionSecretProtector> logger)
    {
        _protector = provider.CreateProtector(Purpose);
        _logger = logger;
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string? Unprotect(string ciphertext)
    {
        try
        {
            return _protector.Unprotect(ciphertext);
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(
                ex, "No fue posible descifrar un secreto guardado en SystemSettings — probablemente cambió la clave de cifrado.");
            return null;
        }
    }
}
