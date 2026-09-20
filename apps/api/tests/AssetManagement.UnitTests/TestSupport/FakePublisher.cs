using MediatR;

namespace AssetManagement.UnitTests.TestSupport;

/// <summary>A genuine no-op — unit tests exercise one handler in isolation and never wire up the
/// notification handlers that would react to a published domain event (that reaction is covered by
/// integration tests instead, which run the real MediatR pipeline end to end).</summary>
internal sealed class FakePublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => Task.CompletedTask;
}
