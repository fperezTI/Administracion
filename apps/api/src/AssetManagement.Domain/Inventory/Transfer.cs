using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Inventory;

/// <summary>
/// Cross-company transfer of an asset (pedido, `docs/multi-company.md`: "aprobación + salida + tránsito +
/// recepción con firma"). Composes F4's generic approval engine and F3's Movement/signature machinery
/// rather than introducing new primitives — this aggregate is only the workflow/status bookkeeping around
/// them. <see cref="Depart"/> collapses "approved" and "departed" into one step: nothing acts on an
/// intermediate "approved but not yet departed" state, so there is no separate status for it — approval
/// completion (see <c>TransferApprovalReactionHandler</c>) triggers departure automatically. Only receipt
/// carries a signature, not departure — see the F5 plan's scope decisions.
/// </summary>
public sealed class Transfer : AuditableAggregateRoot<Guid>
{
    public Guid AssetId { get; private set; }
    public Guid FromCompanyId { get; private set; }
    public Guid ToCompanyId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public TransferStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public Guid? DepartureMovementId { get; private set; }
    public Guid? ReceiptMovementId { get; private set; }
    public Guid? ReceiptSignatureRecordId { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; private set; }
    public DateTimeOffset? DepartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private Transfer()
    {
    }

    private Transfer(
        Guid id, Guid assetId, Guid fromCompanyId, Guid toCompanyId, Guid requestedByUserId, string? notes,
        DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        AssetId = assetId;
        FromCompanyId = fromCompanyId;
        ToCompanyId = toCompanyId;
        RequestedByUserId = requestedByUserId;
        Status = TransferStatus.PendingApproval;
        Notes = notes?.Trim();
        RequestedAtUtc = nowUtc;
    }

    public static Transfer Create(
        Guid assetId, Guid fromCompanyId, Guid toCompanyId, Guid requestedByUserId, string? notes,
        DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        if (fromCompanyId == toCompanyId)
        {
            throw new DomainException("La empresa destino debe ser distinta de la empresa de origen.");
        }

        return new Transfer(Guid.NewGuid(), assetId, fromCompanyId, toCompanyId, requestedByUserId, notes, nowUtc, createdByUserId);
    }

    public void Depart(Guid departureMovementId, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != TransferStatus.PendingApproval)
        {
            throw new DomainException("Solo una transferencia pendiente de aprobación puede salir de la empresa origen.");
        }

        Status = TransferStatus.InTransit;
        DepartureMovementId = departureMovementId;
        DepartedAtUtc = nowUtc;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void Reject(DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != TransferStatus.PendingApproval)
        {
            throw new DomainException("Solo una transferencia pendiente de aprobación puede rechazarse.");
        }

        Status = TransferStatus.Rejected;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void Cancel(Guid callerUserId, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != TransferStatus.PendingApproval)
        {
            throw new DomainException("Solo una transferencia pendiente de aprobación puede cancelarse.");
        }

        if (callerUserId != RequestedByUserId)
        {
            throw new DomainException("Solo quien solicitó la transferencia puede cancelarla.");
        }

        Status = TransferStatus.Cancelled;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void Receive(Guid receiptMovementId, Guid receiptSignatureRecordId, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != TransferStatus.InTransit)
        {
            throw new DomainException("Solo una transferencia en tránsito puede recibirse.");
        }

        Status = TransferStatus.Completed;
        ReceiptMovementId = receiptMovementId;
        ReceiptSignatureRecordId = receiptSignatureRecordId;
        CompletedAtUtc = nowUtc;
        RecordUpdate(nowUtc, updatedByUserId);
    }
}
