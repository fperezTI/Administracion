namespace AssetManagement.Domain.SharedKernel;

/// <summary>Marker for events raised by aggregates and dispatched after persistence succeeds.</summary>
public interface IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; }
}
