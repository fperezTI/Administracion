using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.UnitTests.TestSupport;

internal sealed class FakeCurrentUserContext : ICurrentUserContext
{
    public bool IsAuthenticated { get; set; } = true;

    public Guid? UserId { get; set; } = Guid.NewGuid();

    public Guid? EntraObjectId { get; set; } = Guid.NewGuid();

    public string? DisplayName { get; set; } = "Test User";

    public string? Email { get; set; } = "test@example.com";

    public string? IpAddress { get; set; } = "127.0.0.1";

    public string? UserAgent { get; set; } = "TestAgent/1.0";

    public string? CorrelationId { get; set; } = "test-correlation-id";
}
