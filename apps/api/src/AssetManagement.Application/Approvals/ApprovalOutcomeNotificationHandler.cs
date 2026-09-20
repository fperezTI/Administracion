using AssetManagement.Application.Common.Events;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Approvals;
using AssetManagement.Domain.Approvals.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Approvals;

/// <summary>
/// Notifies the original requester of the outcome of their approval — generic over <c>ContextType</c>,
/// same reasoning as <see cref="ApprovalRequestedNotificationHandler"/>. Deliberately does not filter by
/// context, unlike the business reaction handlers (<c>AssetApprovalReactionHandler</c>,
/// <c>TransferApprovalReactionHandler</c>, <c>InternalRequestApprovalReactionHandler</c>) which each act
/// on their own module — multiple handlers subscribing to the same event is already an established
/// pattern in this codebase.
/// </summary>
public sealed class ApprovalOutcomeNotificationHandler(IApplicationDbContext db, INotificationSender notificationSender)
    : INotificationHandler<DomainEventNotification<ApprovalCompleted>>,
      INotificationHandler<DomainEventNotification<ApprovalRejected>>
{
    public async Task Handle(DomainEventNotification<ApprovalCompleted> notification, CancellationToken cancellationToken)
    {
        await NotifyAsync(notification.DomainEvent.ApprovalInstanceId, "ApprovalCompleted", "Tu solicitud fue aprobada",
            "La aprobación que solicitaste fue completada.", cancellationToken);
    }

    public async Task Handle(DomainEventNotification<ApprovalRejected> notification, CancellationToken cancellationToken)
    {
        await NotifyAsync(notification.DomainEvent.ApprovalInstanceId, "ApprovalRejected", "Tu solicitud fue rechazada",
            "La aprobación que solicitaste fue rechazada.", cancellationToken);
    }

    private async Task NotifyAsync(Guid approvalInstanceId, string type, string title, string body, CancellationToken cancellationToken)
    {
        var instance = await db.ApprovalInstances.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == approvalInstanceId, cancellationToken);
        if (instance is null)
        {
            return;
        }

        await notificationSender.NotifyAsync(instance.RequestedByUserId, type, title, body, instance.CompanyId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
