using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Inventory;

/// <summary>
/// Custody of an asset by a specific user. Deliberately two-step (see docs/architecture/domain-model.md):
/// created as <see cref="AssignmentStatus.PendingSignature"/> — the asset only reaches
/// <c>AssetStatus.Assigned</c> once the recipient signs their own acceptance via
/// <see cref="Accept"/>, never at creation time. Returning also requires a signature (this time from
/// whoever executes the return, not the outgoing custodian — see the F3 plan's scope decisions).
/// </summary>
public sealed class Assignment : AuditableAggregateRoot<Guid>
{
    public Guid CompanyId { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid AssignedToUserId { get; private set; }
    public Guid? OrgUnitId { get; private set; }
    public Guid MovementId { get; private set; }
    public AssignmentStatus Status { get; private set; }
    public Guid? SignatureRecordId { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? ReturnedAtUtc { get; private set; }
    public Guid? ReturnMovementId { get; private set; }
    public Guid? ReturnSignatureRecordId { get; private set; }

    private Assignment()
    {
    }

    private Assignment(
        Guid id, Guid companyId, Guid assetId, Guid assignedToUserId, Guid? orgUnitId, Guid movementId,
        DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        CompanyId = companyId;
        AssetId = assetId;
        AssignedToUserId = assignedToUserId;
        OrgUnitId = orgUnitId;
        MovementId = movementId;
        Status = AssignmentStatus.PendingSignature;
        AssignedAtUtc = nowUtc;
    }

    public static Assignment Create(
        Guid companyId, Guid assetId, Guid assignedToUserId, Guid? orgUnitId, Guid movementId,
        DateTimeOffset nowUtc, Guid? createdByUserId) =>
        new(Guid.NewGuid(), companyId, assetId, assignedToUserId, orgUnitId, movementId, nowUtc, createdByUserId);

    public void Accept(Guid signatureRecordId, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != AssignmentStatus.PendingSignature)
        {
            throw new DomainException("Solo una asignación pendiente de firma puede aceptarse.");
        }

        Status = AssignmentStatus.Accepted;
        SignatureRecordId = signatureRecordId;
        AcceptedAtUtc = nowUtc;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void Cancel(DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != AssignmentStatus.PendingSignature)
        {
            throw new DomainException("Solo una asignación pendiente de firma puede cancelarse.");
        }

        Status = AssignmentStatus.Cancelled;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void Return(Guid returnSignatureRecordId, Guid returnMovementId, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != AssignmentStatus.Accepted)
        {
            throw new DomainException("Solo una asignación aceptada puede devolverse.");
        }

        Status = AssignmentStatus.Returned;
        ReturnSignatureRecordId = returnSignatureRecordId;
        ReturnMovementId = returnMovementId;
        ReturnedAtUtc = nowUtc;
        RecordUpdate(nowUtc, updatedByUserId);
    }
}
