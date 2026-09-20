namespace AssetManagement.Domain.SharedKernel;

/// <summary>Non-generic view of <see cref="AggregateRoot{TId}"/> so Infrastructure can collect raised
/// events across every aggregate type in one <c>ChangeTracker</c> scan (<c>AggregateRoot&lt;TId&gt;</c>
/// itself can't be used that way — <c>TId</c> varies per aggregate).</summary>
public interface IHasDomainEvents
{
    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    public void ClearDomainEvents();
}
