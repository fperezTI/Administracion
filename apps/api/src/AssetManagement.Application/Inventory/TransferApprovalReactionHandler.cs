using AssetManagement.Application.Common.Events;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Approvals.Events;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>
/// Reacts to the cross-company transfer approval outcome (pedido §10 pattern, same as
/// <see cref="AssetApprovalReactionHandler"/>). Unlike the decommission/disposal handlers, the context id
/// here is the <see cref="Domain.Inventory.Transfer"/>'s own id, not the asset's — a transfer needs its
/// own bookkeeping (<c>DepartureMovementId</c>, status) beyond just the asset's status, so it is the
/// natural context root. On completion this is what actually makes the asset leave the source company's
/// warehouse ("salida") — departure has no separate manual step, see the F5 plan.
/// </summary>
public sealed class TransferApprovalReactionHandler(IApplicationDbContext db, IFolioGenerator folioGenerator, IClock clock)
    : INotificationHandler<DomainEventNotification<ApprovalCompleted>>,
      INotificationHandler<DomainEventNotification<ApprovalRejected>>
{
    public async Task Handle(DomainEventNotification<ApprovalCompleted> notification, CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;
        if (evt.ContextType != "CrossCompanyTransfer")
        {
            return;
        }

        var transfer = await db.Transfers.FirstOrDefaultAsync(t => t.Id == evt.ContextId, cancellationToken);
        if (transfer is null)
        {
            return;
        }

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == transfer.AssetId, cancellationToken);
        if (asset is null)
        {
            return;
        }

        var now = clock.UtcNow;
        var folio = await folioGenerator.NextAsync(transfer.FromCompanyId, FolioDocumentTypes.MovementCrossCompanyTransferOut, cancellationToken);

        var departureMovement = Domain.Inventory.Movement.Create(
            transfer.FromCompanyId, asset.Id, Domain.Inventory.MovementType.CrossCompanyTransferOut, folio,
            startsCompleted: true, fromOrgUnitId: asset.CurrentOrgUnitId, toOrgUnitId: null, fromUserId: null,
            toUserId: null, notes: transfer.Notes, now, null, toCompanyId: transfer.ToCompanyId);

        asset.ChangeStatus(AssetStatus.InTransit, now, null);
        transfer.Depart(departureMovement.Id, now, null);

        db.Movements.Add(departureMovement);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(DomainEventNotification<ApprovalRejected> notification, CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;
        if (evt.ContextType != "CrossCompanyTransfer")
        {
            return;
        }

        var transfer = await db.Transfers.FirstOrDefaultAsync(t => t.Id == evt.ContextId, cancellationToken);
        if (transfer is null)
        {
            return;
        }

        // The asset never left InWarehouse — nothing to undo there, only the Transfer's own status.
        transfer.Reject(clock.UtcNow, null);
        await db.SaveChangesAsync(cancellationToken);
    }
}
