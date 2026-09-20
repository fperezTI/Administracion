using AssetManagement.Domain.Notifications;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Notifications;

public class NotificationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_starts_unread()
    {
        var notification = Notification.Create(Guid.NewGuid(), "ApprovalRequested", "Tienes una aprobación pendiente", "Cuerpo", null, Now);

        notification.IsRead.Should().BeFalse();
        notification.ReadAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_rejects_empty_title()
    {
        var act = () => Notification.Create(Guid.NewGuid(), "ApprovalRequested", "  ", "Cuerpo", null, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkAsRead_sets_read_state()
    {
        var notification = Notification.Create(Guid.NewGuid(), "ApprovalRequested", "Título", "Cuerpo", null, Now);

        notification.MarkAsRead(Now.AddMinutes(5));

        notification.IsRead.Should().BeTrue();
        notification.ReadAtUtc.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public void MarkAsRead_twice_keeps_the_first_read_timestamp()
    {
        var notification = Notification.Create(Guid.NewGuid(), "ApprovalRequested", "Título", "Cuerpo", null, Now);
        notification.MarkAsRead(Now.AddMinutes(5));

        notification.MarkAsRead(Now.AddMinutes(10));

        notification.ReadAtUtc.Should().Be(Now.AddMinutes(5));
    }
}
