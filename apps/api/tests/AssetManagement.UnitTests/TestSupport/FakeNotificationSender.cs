using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.UnitTests.TestSupport;

internal sealed record RecordedNotification(
    Guid UserId, string Type, string Title, string Body, Guid? CompanyId, string? EmailBodyHtml);

/// <summary>Records calls instead of touching a real db/email — handler tests only need to assert who got
/// notified and how many times, not the actual delivery (that's GraphEmailSender/SmtpEmailSender's job,
/// neither of which has its own unit test either — thin IO adapters, see FakeFileStorage).</summary>
internal sealed class FakeNotificationSender : INotificationSender
{
    public List<RecordedNotification> Notifications { get; } = [];

    public Task NotifyAsync(
        Guid userId, string type, string title, string body, Guid? companyId, CancellationToken cancellationToken,
        string? emailBodyHtml = null)
    {
        Notifications.Add(new RecordedNotification(userId, type, title, body, companyId, emailBodyHtml));
        return Task.CompletedTask;
    }
}
