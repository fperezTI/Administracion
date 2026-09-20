using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>Lets an administrator cancel an assignment nobody has signed yet — otherwise a pending
/// assignment with a non-responsive recipient would leave the asset stuck in Reserved forever.</summary>
public sealed record CancelPendingAssignmentCommand(Guid AssignmentId) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assignments.Update;
}

public sealed class CancelPendingAssignmentCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<CancelPendingAssignmentCommand>
{
    public async Task Handle(CancelPendingAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments.FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Assignment), request.AssignmentId);

        if (!currentCompany.AccessibleCompanyIds.Contains(assignment.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de esta asignación.");
        }

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assignment.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), assignment.AssetId);

        var movement = await db.Movements.FirstOrDefaultAsync(m => m.Id == assignment.MovementId, cancellationToken)
            ?? throw new NotFoundException(nameof(Movement), assignment.MovementId);

        var now = clock.UtcNow;
        assignment.Cancel(now, currentUser.UserId);
        movement.Cancel(now, currentUser.UserId);
        asset.ChangeStatus(AssetStatus.InWarehouse, now, currentUser.UserId);

        await db.SaveChangesAsync(cancellationToken);
    }
}
