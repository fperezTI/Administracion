using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Notifications;

public sealed record MarkNotificationAsReadCommand(Guid NotificationId) : IRequest
{
}

public sealed class MarkNotificationAsReadCommandHandler(IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<MarkNotificationAsReadCommand>
{
    public async Task Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión.");
        }

        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == request.NotificationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);

        if (notification.UserId != userId)
        {
            throw new ForbiddenAccessException("No puedes marcar como leída la notificación de otra persona.");
        }

        notification.MarkAsRead(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }
}
