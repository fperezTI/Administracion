using AssetManagement.Application.Common.Events;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Approvals.Events;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>
/// Reacts to the two asset-related approval outcomes (pedido §10: the requesting module "se suscribe al
/// evento de dominio... para continuar su propio flujo" — Approvals itself never imports this assembly).
/// Decommission bounces back to <see cref="AssetStatus.InWarehouse"/> on rejection (see the F4 plan's
/// AssetStateMachine change); disposal simply does nothing on rejection since
/// <see cref="AssetStatus.Decommissioned"/> was never left.
/// </summary>
public sealed class AssetApprovalReactionHandler(IApplicationDbContext db, IClock clock)
    : INotificationHandler<DomainEventNotification<ApprovalCompleted>>,
      INotificationHandler<DomainEventNotification<ApprovalRejected>>
{
    public async Task Handle(DomainEventNotification<ApprovalCompleted> notification, CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;
        if (evt.ContextType == "AssetDecommission")
        {
            await TransitionAsync(evt.ContextId, AssetStatus.Decommissioned, cancellationToken);
        }
        else if (evt.ContextType.StartsWith("AssetDisposal:", StringComparison.Ordinal))
        {
            var targetStatus = Enum.Parse<AssetStatus>(evt.ContextType["AssetDisposal:".Length..]);
            await TransitionAsync(evt.ContextId, targetStatus, cancellationToken);
        }
    }

    public async Task Handle(DomainEventNotification<ApprovalRejected> notification, CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;
        if (evt.ContextType == "AssetDecommission")
        {
            await TransitionAsync(evt.ContextId, AssetStatus.InWarehouse, cancellationToken);
        }

        // AssetDisposal:* rejections are a no-op — the asset never left Decommissioned.
    }

    private async Task TransitionAsync(Guid assetId, AssetStatus targetStatus, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken);
        if (asset is null)
        {
            return;
        }

        asset.ChangeStatus(targetStatus, clock.UtcNow, null);
        await db.SaveChangesAsync(cancellationToken);
    }
}
