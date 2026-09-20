using AssetManagement.Application.Common.Events;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Approvals.Events;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using AssetManagement.Domain.Maintenance;
using AssetManagement.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Requests;

/// <summary>
/// Reacts to the internal-request approval outcome (same skeleton as
/// <see cref="Inventory.TransferApprovalReactionHandler"/>, F5). On completion, reconstructs — by
/// <see cref="InternalRequest.Type"/> — the same domain sequence <c>CreateAssignmentCommand</c>/
/// <c>CreateLoanCommand</c>/<c>OpenMaintenanceOrderCommand</c> already run, rather than re-invoking those
/// commands via <c>ISender</c> (Application handlers never call each other that way — see the F7 plan,
/// decision 3). The requester is always the beneficiary (<see cref="InternalRequest.RequestedByUserId"/>):
/// this is self-service ("assign this to me"), not a way to originate an action for someone else.
/// </summary>
public sealed class InternalRequestApprovalReactionHandler(IApplicationDbContext db, IFolioGenerator folioGenerator, IClock clock)
    : INotificationHandler<DomainEventNotification<ApprovalCompleted>>,
      INotificationHandler<DomainEventNotification<ApprovalRejected>>
{
    public async Task Handle(DomainEventNotification<ApprovalCompleted> notification, CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;
        if (evt.ContextType != "InternalRequest")
        {
            return;
        }

        var request = await db.InternalRequests.FirstOrDefaultAsync(r => r.Id == evt.ContextId, cancellationToken);
        if (request is null)
        {
            return;
        }

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken);
        if (asset is null)
        {
            return;
        }

        var now = clock.UtcNow;
        Guid fulfillmentReferenceId;

        switch (request.Type)
        {
            case InternalRequestType.AssetAssignment:
            {
                var folio = await folioGenerator.NextAsync(asset.CompanyId, FolioDocumentTypes.MovementAssignment, cancellationToken);
                var movement = Movement.Create(
                    asset.CompanyId, asset.Id, MovementType.Assignment, folio, startsCompleted: false,
                    fromOrgUnitId: asset.CurrentOrgUnitId, toOrgUnitId: null, fromUserId: null,
                    toUserId: request.RequestedByUserId, notes: request.Justification, now, null);
                var assignment = Assignment.Create(
                    asset.CompanyId, asset.Id, request.RequestedByUserId, orgUnitId: null, movement.Id, now, null);

                asset.ChangeStatus(AssetStatus.Reserved, now, null);

                db.Movements.Add(movement);
                db.Assignments.Add(assignment);
                fulfillmentReferenceId = assignment.Id;
                break;
            }
            case InternalRequestType.Loan:
            {
                var folio = await folioGenerator.NextAsync(asset.CompanyId, FolioDocumentTypes.MovementLoan, cancellationToken);
                var movement = Movement.Create(
                    asset.CompanyId, asset.Id, MovementType.Loan, folio, startsCompleted: true,
                    fromOrgUnitId: asset.CurrentOrgUnitId, toOrgUnitId: null, fromUserId: null,
                    toUserId: request.RequestedByUserId, notes: request.Justification, now, null);
                var loan = Loan.Create(
                    asset.CompanyId, asset.Id, request.RequestedByUserId, movement.Id, request.ExpectedReturnDate!.Value,
                    now, null);

                asset.ChangeStatus(AssetStatus.OnLoan, now, null);

                db.Movements.Add(movement);
                db.Loans.Add(loan);
                fulfillmentReferenceId = loan.Id;
                break;
            }
            case InternalRequestType.Maintenance:
            {
                var folio = await folioGenerator.NextAsync(asset.CompanyId, FolioDocumentTypes.MaintenanceOrder, cancellationToken);
                var order = MaintenanceOrder.Open(
                    asset.CompanyId, asset.Id, folio, MaintenanceOrderType.Corrective, request.Justification,
                    checklistDefinitionId: null, checklistVersionNumber: null, checklistItems: null, now, null);

                asset.ChangeStatus(AssetStatus.InMaintenance, now, null);

                db.MaintenanceOrders.Add(order);
                fulfillmentReferenceId = order.Id;
                break;
            }
            default:
                return;
        }

        request.Fulfill(fulfillmentReferenceId, now, null);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(DomainEventNotification<ApprovalRejected> notification, CancellationToken cancellationToken)
    {
        var evt = notification.DomainEvent;
        if (evt.ContextType != "InternalRequest")
        {
            return;
        }

        var request = await db.InternalRequests.FirstOrDefaultAsync(r => r.Id == evt.ContextId, cancellationToken);
        if (request is null)
        {
            return;
        }

        // The asset never changed state while pending — nothing to undo there, only the request's own status.
        request.Reject(clock.UtcNow, null);
        await db.SaveChangesAsync(cancellationToken);
    }
}
