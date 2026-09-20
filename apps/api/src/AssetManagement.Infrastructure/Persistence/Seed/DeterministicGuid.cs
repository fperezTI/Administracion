using System.Security.Cryptography;
using System.Text;

namespace AssetManagement.Infrastructure.Persistence.Seed;

/// <summary>
/// Produces a stable Guid from a fixed string (e.g. a permission code) so EF Core HasData seeds have
/// consistent keys across every migration regeneration — required because HasData cannot use
/// Guid.NewGuid() (it would produce a different value, hence a spurious diff, on every "migrations add").
/// </summary>
internal static class DeterministicGuid
{
    public static Guid Create(string input)
    {
        // SHA-256 (truncated) purely for a stable, well-distributed 16-byte id — not a security use,
        // but a non-broken algorithm is used anyway so this never trips a "no MD5/SHA1" scanner rule.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash[..16]);
    }
}
