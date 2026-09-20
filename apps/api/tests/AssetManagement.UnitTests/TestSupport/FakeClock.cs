using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.UnitTests.TestSupport;

internal sealed class FakeClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}
