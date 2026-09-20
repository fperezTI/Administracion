using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Inventory;

/// <summary>
/// The audited log entry for anything that changes where an asset is or who has it (pedido §13/§29,
/// CLAUDE.md regla 6: "un Movement completado no se edita ni se borra"). Created either already
/// <see cref="MovementStatus.Completed"/> (single-step operations: Loan, LoanReturn, Relocation) or
/// <see cref="MovementStatus.Pending"/> (Assignment, which only completes once the recipient signs —
/// see <see cref="Assignment"/>). Once <see cref="Completed"/> or <see cref="Cancelled"/> it is frozen;
/// corrections are new compensating movements, never edits to this one.
/// </summary>
public sealed class Movement : AuditableAggregateRoot<Guid>
{
    public Guid CompanyId { get; private set; }
    public Guid AssetId { get; private set; }
    public MovementType Type { get; private set; }
    public string FolioNumber { get; private set; } = null!;
    public MovementStatus Status { get; private set; }
    public Guid? FromOrgUnitId { get; private set; }
    public Guid? ToOrgUnitId { get; private set; }
    public Guid? FromUserId { get; private set; }
    public Guid? ToUserId { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset EffectiveAtUtc { get; private set; }

    /// <summary>Informative only (F5) — the "other side" company of a cross-company transfer.
    /// <see cref="CompanyId"/> stays the row's one owning company for query-filter purposes; a transfer
    /// produces two Movement rows, one per company, because the global query filter is single-company
    /// per row (see AppDbContext.OnModelCreating's OR-filter on Transfer for the one place that genuinely
    /// needs two-company visibility).</summary>
    public Guid? FromCompanyId { get; private set; }
    public Guid? ToCompanyId { get; private set; }

    private Movement()
    {
    }

    private Movement(
        Guid id, Guid companyId, Guid assetId, MovementType type, string folioNumber, MovementStatus status,
        Guid? fromOrgUnitId, Guid? toOrgUnitId, Guid? fromUserId, Guid? toUserId, string? notes,
        DateTimeOffset nowUtc, Guid? createdByUserId, Guid? fromCompanyId, Guid? toCompanyId)
        : base(id, nowUtc, createdByUserId)
    {
        CompanyId = companyId;
        AssetId = assetId;
        Type = type;
        FolioNumber = folioNumber;
        Status = status;
        FromOrgUnitId = fromOrgUnitId;
        ToOrgUnitId = toOrgUnitId;
        FromUserId = fromUserId;
        ToUserId = toUserId;
        Notes = notes;
        EffectiveAtUtc = nowUtc;
        FromCompanyId = fromCompanyId;
        ToCompanyId = toCompanyId;
    }

    public static Movement Create(
        Guid companyId, Guid assetId, MovementType type, string folioNumber, bool startsCompleted,
        Guid? fromOrgUnitId, Guid? toOrgUnitId, Guid? fromUserId, Guid? toUserId, string? notes,
        DateTimeOffset nowUtc, Guid? createdByUserId, Guid? fromCompanyId = null, Guid? toCompanyId = null)
    {
        if (string.IsNullOrWhiteSpace(folioNumber))
        {
            throw new DomainException("El folio del movimiento es obligatorio.");
        }

        return new Movement(
            Guid.NewGuid(), companyId, assetId, type, folioNumber,
            startsCompleted ? MovementStatus.Completed : MovementStatus.Pending, fromOrgUnitId, toOrgUnitId,
            fromUserId, toUserId, notes?.Trim(), nowUtc, createdByUserId, fromCompanyId, toCompanyId);
    }

    public void Complete(DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != MovementStatus.Pending)
        {
            throw new DomainException("Solo un movimiento pendiente puede completarse.");
        }

        Status = MovementStatus.Completed;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void Cancel(DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != MovementStatus.Pending)
        {
            throw new DomainException("Solo un movimiento pendiente puede cancelarse.");
        }

        Status = MovementStatus.Cancelled;
        RecordUpdate(nowUtc, updatedByUserId);
    }
}
