using AssetManagement.Application.Common.Events;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Approvals.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Approvals;

/// <summary>
/// Notifies every user eligible to decide a newly-created <see cref="Domain.Approvals.ApprovalInstance"/>
/// (F8's notification center) — generic over <c>ContextType</c>, unlike the business reaction handlers
/// (F4/F5/F7), so it covers decommission/disposal, transfers and internal requests automatically without
/// touching those modules. V1 simplification: notifies every role holder regardless of
/// <see cref="Domain.Approvals.ApprovalMode"/> turn order (see the F8 plan, decision 2).
/// </summary>
public sealed class ApprovalRequestedNotificationHandler(IApplicationDbContext db, INotificationSender notificationSender)
    : INotificationHandler<DomainEventNotification<ApprovalRequested>>
{
    public async Task Handle(DomainEventNotification<ApprovalRequested> notification, CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;

        var approverUserIds = await db.UserRoles
            .Where(ur => evt.ApproverRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (approverUserIds.Count == 0)
        {
            return;
        }

        var eligibleUserIds = await db.UserCompanies
            .Where(uc => uc.CompanyId == evt.CompanyId && approverUserIds.Contains(uc.UserId) && uc.UserId != evt.RequestedByUserId)
            .Select(uc => uc.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var userId in eligibleUserIds)
        {
            await notificationSender.NotifyAsync(
                userId, "ApprovalRequested", "Tienes una aprobación pendiente",
                "Alguien solicitó una aprobación que puedes decidir. Revisa Mis aprobaciones.", evt.CompanyId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
