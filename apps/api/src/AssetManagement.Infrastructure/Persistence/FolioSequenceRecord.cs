namespace AssetManagement.Infrastructure.Persistence;

/// <summary>
/// Purely technical row backing IFolioGenerator's atomic increment (see EfFolioGenerator) — a monotonic
/// counter has no business behavior of its own, so it is not a Domain aggregate (pedido §8/§17's
/// "prefijos y secuencias de folios"). Not exposed on IApplicationDbContext: Application only sees the
/// IFolioGenerator port.
/// </summary>
public sealed class FolioSequenceRecord
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string DocumentType { get; private set; } = null!;
    public int NextValue { get; private set; }
}
