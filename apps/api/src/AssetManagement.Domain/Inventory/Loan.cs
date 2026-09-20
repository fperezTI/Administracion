using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Inventory;

/// <summary>
/// Short-term custody of an asset, unlike <see cref="Assignment"/> deliberately single-step and
/// signature-free (docs/architecture/domain-model.md only requires a signature for Assignment) — a
/// loan is meant for informal, short-lived borrowing (e.g. a projector for a meeting), not the formal
/// long-term custody an Assignment represents.
/// </summary>
public sealed class Loan : AuditableAggregateRoot<Guid>
{
    public Guid CompanyId { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid BorrowerUserId { get; private set; }
    public Guid MovementId { get; private set; }
    public DateOnly ExpectedReturnDate { get; private set; }
    public LoanStatus Status { get; private set; }
    public DateTimeOffset LoanedAtUtc { get; private set; }
    public DateTimeOffset? ReturnedAtUtc { get; private set; }
    public Guid? ReturnMovementId { get; private set; }

    private Loan()
    {
    }

    private Loan(
        Guid id, Guid companyId, Guid assetId, Guid borrowerUserId, Guid movementId,
        DateOnly expectedReturnDate, DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        CompanyId = companyId;
        AssetId = assetId;
        BorrowerUserId = borrowerUserId;
        MovementId = movementId;
        ExpectedReturnDate = expectedReturnDate;
        Status = LoanStatus.Active;
        LoanedAtUtc = nowUtc;
    }

    public static Loan Create(
        Guid companyId, Guid assetId, Guid borrowerUserId, Guid movementId, DateOnly expectedReturnDate,
        DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        if (expectedReturnDate < DateOnly.FromDateTime(nowUtc.UtcDateTime))
        {
            throw new DomainException("La fecha esperada de devolución no puede ser en el pasado.");
        }

        return new Loan(Guid.NewGuid(), companyId, assetId, borrowerUserId, movementId, expectedReturnDate, nowUtc, createdByUserId);
    }

    public void Return(Guid returnMovementId, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != LoanStatus.Active)
        {
            throw new DomainException("Solo un préstamo activo puede devolverse.");
        }

        Status = LoanStatus.Returned;
        ReturnMovementId = returnMovementId;
        ReturnedAtUtc = nowUtc;
        RecordUpdate(nowUtc, updatedByUserId);
    }
}
