using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Maintenance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record SetMaintenanceChecklistDefinitionActiveCommand(Guid ChecklistDefinitionId, bool IsActive)
    : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Maintenance.Update;
}

public sealed class SetMaintenanceChecklistDefinitionActiveCommandHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<SetMaintenanceChecklistDefinitionActiveCommand>
{
    public async Task Handle(SetMaintenanceChecklistDefinitionActiveCommand request, CancellationToken cancellationToken)
    {
        var checklist = await db.MaintenanceChecklistDefinitions
            .FirstOrDefaultAsync(c => c.Id == request.ChecklistDefinitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(MaintenanceChecklistDefinition), request.ChecklistDefinitionId);

        checklist.SetActive(request.IsActive, clock.UtcNow, currentUser.UserId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
