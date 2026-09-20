using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Assets;

/// <summary>
/// The physical identification issued to an asset (pedido §12). <see cref="Code"/> is the asset's
/// identity on the label and never changes; reprinting only records a new print event
/// (<see cref="RecordReprint"/>), it does not reissue a new code.
/// </summary>
public sealed class AssetTag : Entity<Guid>
{
    public Guid AssetId { get; private set; }
    public string Code { get; private set; } = null!;
    public IdentificationTechnology Technology { get; private set; }
    public int PrintCount { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset LastPrintedAtUtc { get; private set; }

    private AssetTag()
    {
    }

    private AssetTag(
        Guid id, Guid assetId, string code, IdentificationTechnology technology, DateTimeOffset nowUtc)
        : base(id)
    {
        AssetId = assetId;
        Code = code;
        Technology = technology;
        PrintCount = 1;
        IssuedAtUtc = nowUtc;
        LastPrintedAtUtc = nowUtc;
    }

    internal static AssetTag Issue(
        Guid assetId, string code, IdentificationTechnology technology, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("El código de la etiqueta es obligatorio.");
        }

        return new AssetTag(Guid.NewGuid(), assetId, code.Trim(), technology, nowUtc);
    }

    public void RecordReprint(DateTimeOffset nowUtc)
    {
        PrintCount += 1;
        LastPrintedAtUtc = nowUtc;
    }
}
