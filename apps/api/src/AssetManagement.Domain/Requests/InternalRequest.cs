using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Requests;

/// <summary>
/// The self-service front door for an employee to ask for something that requires approval (pedido:
/// "solicitudes internas con aprobación configurable"). Does not introduce new primitives — approving one
/// triggers the same domain sequence <see cref="Inventory.Assignment"/>/<see cref="Inventory.Loan"/>/
/// <see cref="Maintenance.MaintenanceOrder"/> already use (see
/// <c>InternalRequestApprovalReactionHandler</c>), exactly like F5 composed F3+F4 for
/// <see cref="Inventory.Transfer"/>. No draft-editing phase, unlike the literal "borrador→...→cerrada"
/// wording in domain-model.md might suggest — <see cref="Create"/> goes straight to
/// <see cref="InternalRequestStatus.PendingApproval"/>, same single-step shape as every other request-like
/// aggregate already built (<see cref="Inventory.Transfer"/>, <see cref="Inventory.Loan"/>,
/// <see cref="Maintenance.MaintenanceOrder"/>) — see the F7 plan, decision 1.
/// </summary>
public sealed class InternalRequest : AuditableAggregateRoot<Guid>
{
    public Guid CompanyId { get; private set; }
    public InternalRequestType Type { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public string Justification { get; private set; } = null!;
    public DateOnly? ExpectedReturnDate { get; private set; }
    public InternalRequestStatus Status { get; private set; }

    /// <summary>Id of the <see cref="Inventory.Assignment"/>/<see cref="Inventory.Loan"/>/
    /// <see cref="Maintenance.MaintenanceOrder"/> created on fulfillment — a single nullable column since
    /// <see cref="Type"/> already says which table to look in (avoids three columns that are almost always
    /// null).</summary>
    public Guid? FulfillmentReferenceId { get; private set; }

    public DateTimeOffset RequestedAtUtc { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }

    private InternalRequest()
    {
    }

    private InternalRequest(
        Guid id, Guid companyId, InternalRequestType type, Guid assetId, Guid requestedByUserId,
        string justification, DateOnly? expectedReturnDate, DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        CompanyId = companyId;
        Type = type;
        AssetId = assetId;
        RequestedByUserId = requestedByUserId;
        Justification = justification;
        ExpectedReturnDate = expectedReturnDate;
        Status = InternalRequestStatus.PendingApproval;
        RequestedAtUtc = nowUtc;
    }

    public static InternalRequest Create(
        Guid companyId, InternalRequestType type, Guid assetId, Guid requestedByUserId, string justification,
        DateOnly? expectedReturnDate, DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(justification))
        {
            throw new DomainException("La justificación de la solicitud es obligatoria.");
        }

        if (type == InternalRequestType.Loan && expectedReturnDate is null)
        {
            throw new DomainException("Una solicitud de préstamo requiere la fecha esperada de devolución.");
        }

        if (type != InternalRequestType.Loan && expectedReturnDate is not null)
        {
            throw new DomainException("La fecha esperada de devolución solo aplica a solicitudes de préstamo.");
        }

        return new InternalRequest(
            Guid.NewGuid(), companyId, type, assetId, requestedByUserId, justification.Trim(), expectedReturnDate,
            nowUtc, createdByUserId);
    }

    public void Reject(DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != InternalRequestStatus.PendingApproval)
        {
            throw new DomainException("Solo una solicitud pendiente de aprobación puede rechazarse.");
        }

        Status = InternalRequestStatus.Rejected;
        DecidedAtUtc = nowUtc;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void Cancel(Guid callerUserId, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != InternalRequestStatus.PendingApproval)
        {
            throw new DomainException("Solo una solicitud pendiente de aprobación puede cancelarse.");
        }

        if (callerUserId != RequestedByUserId)
        {
            throw new DomainException("Solo quien solicitó puede cancelar su propia solicitud.");
        }

        Status = InternalRequestStatus.Cancelled;
        DecidedAtUtc = nowUtc;
        RecordUpdate(nowUtc, updatedByUserId);
    }

    public void Fulfill(Guid fulfillmentReferenceId, DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != InternalRequestStatus.PendingApproval)
        {
            throw new DomainException("Solo una solicitud pendiente de aprobación puede cumplirse.");
        }

        Status = InternalRequestStatus.Fulfilled;
        FulfillmentReferenceId = fulfillmentReferenceId;
        DecidedAtUtc = nowUtc;
        RecordUpdate(nowUtc, updatedByUserId);
    }
}
