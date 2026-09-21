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

        var now = clock.UtcNow;

        var groupMembers = await AssignmentGroupSupport.GetGroupMembersAsync(
            db, assignment, AssignmentStatus.PendingSignature, cancellationToken);

        foreach (var member in groupMembers)
        {
            var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == member.AssetId, cancellationToken)
                ?? throw new NotFoundException(nameof(Asset), member.AssetId);

            var movement = await db.Movements.FirstOrDefaultAsync(m => m.Id == member.MovementId, cancellationToken)
                ?? throw new NotFoundException(nameof(Movement), member.MovementId);

            member.Cancel(now, currentUser.UserId);
            movement.Cancel(now, currentUser.UserId);
            asset.ChangeStatus(AssetStatus.InWarehouse, now, currentUser.UserId);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
