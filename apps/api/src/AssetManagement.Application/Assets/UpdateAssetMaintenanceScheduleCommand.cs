using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets;

/// <summary>A quick-glance "próximo mantenimiento" date, independent of the actual MaintenanceOrder
/// history (F6) — lets list/detail views flag upcoming servicing without joining that module.</summary>
public sealed record UpdateAssetMaintenanceScheduleCommand(Guid AssetId, DateOnly? NextMaintenanceDueDate)
    : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assets.Update;
}

public sealed class UpdateAssetMaintenanceScheduleCommandHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<UpdateAssetMaintenanceScheduleCommand>
{
    public async Task Handle(UpdateAssetMaintenanceScheduleCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        asset.SetNextMaintenanceDueDate(request.NextMaintenanceDueDate, clock.UtcNow, currentUser.UserId);

        await db.SaveChangesAsync(cancellationToken);
    }
}
