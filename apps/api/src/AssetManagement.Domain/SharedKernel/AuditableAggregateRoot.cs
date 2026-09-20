namespace AssetManagement.Domain.SharedKernel;

/// <summary>
/// Adds the creation/modification audit trail pedido §11 requires on Asset ("fecha de creación, usuario
/// creador, fecha de modificación, usuario modificador, versión de concurrencia"). A separate base from
/// <see cref="AggregateRoot{TId}"/> rather than adding these fields there, so F1 aggregates (User, Role,
/// Company, OrgUnit) are not retroactively migrated for a field they don't yet need — see
/// docs/roadmap.md. New aggregates that need this trail should inherit from this type directly.
/// </summary>
public abstract class AuditableAggregateRoot<TId> : AggregateRoot<TId>
    where TId : notnull
{
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    protected AuditableAggregateRoot()
    {
    }

    protected AuditableAggregateRoot(TId id, DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id)
    {
        CreatedAtUtc = nowUtc;
        CreatedByUserId = createdByUserId;
    }

    protected void RecordUpdate(DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        UpdatedAtUtc = nowUtc;
        UpdatedByUserId = updatedByUserId;
    }
}
