namespace AssetManagement.Application.Common.Interfaces;

/// <summary>All domain/application timestamps are UTC and obtained through this port, never DateTime.Now.</summary>
public interface IClock
{
    public DateTimeOffset UtcNow { get; }
}
