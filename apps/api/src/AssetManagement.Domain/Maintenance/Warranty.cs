using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Maintenance;

/// <summary>
/// One warranty/support coverage period for an asset (pedido: "garantía"), gated by the
/// <c>Warranties.*</c> permission module — sembrado since F1, unused until F6. Deliberately separate from
/// <see cref="Assets.Asset"/>'s own <c>WarrantyStartDate</c>/<c>WarrantyEndDate</c>/<c>SupportContract</c>
/// fields (F2, gated by <c>Assets.Update</c>, left untouched): an asset can accumulate several coverages
/// over its life (original manufacturer warranty, later an extended one purchased separately, ...), which
/// a single flat pair of dates cannot represent — see the F6 plan.
/// </summary>
public sealed class Warranty : AuditableAggregateRoot<Guid>
{
    public Guid CompanyId { get; private set; }
    public Guid AssetId { get; private set; }
    public WarrantyType Type { get; private set; }
    public string Provider { get; private set; } = null!;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string? Terms { get; private set; }

    private Warranty()
    {
    }

    private Warranty(
        Guid id, Guid companyId, Guid assetId, WarrantyType type, string provider, DateOnly startDate,
        DateOnly endDate, string? terms, DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        CompanyId = companyId;
        AssetId = assetId;
        Type = type;
        Provider = provider;
        StartDate = startDate;
        EndDate = endDate;
        Terms = terms;
    }

    public static Warranty Create(
        Guid companyId, Guid assetId, WarrantyType type, string provider, DateOnly startDate, DateOnly endDate,
        string? terms, DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        Validate(provider, startDate, endDate);

        return new Warranty(
            Guid.NewGuid(), companyId, assetId, type, provider.Trim(), startDate, endDate, terms?.Trim(), nowUtc,
            createdByUserId);
    }

    public void UpdateDetails(
        WarrantyType type, string provider, DateOnly startDate, DateOnly endDate, string? terms,
        DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        Validate(provider, startDate, endDate);

        Type = type;
        Provider = provider.Trim();
        StartDate = startDate;
        EndDate = endDate;
        Terms = terms?.Trim();
        RecordUpdate(nowUtc, updatedByUserId);
    }

    private static void Validate(string provider, DateOnly startDate, DateOnly endDate)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new DomainException("El proveedor de la garantía es obligatorio.");
        }

        if (startDate > endDate)
        {
            throw new DomainException("La fecha de inicio de la garantía debe ser anterior o igual a la fecha de fin.");
        }
    }
}
