using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.Infrastructure.Common;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
