using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Organization.OrgUnits;

public sealed record SetOrgUnitActiveCommand(Guid OrgUnitId, bool IsActive) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Structure.Update;
}

public sealed class SetOrgUnitActiveCommandHandler(IApplicationDbContext db) : IRequestHandler<SetOrgUnitActiveCommand>
{
    public async Task Handle(SetOrgUnitActiveCommand request, CancellationToken cancellationToken)
    {
        var orgUnit = await db.OrgUnits.FirstOrDefaultAsync(o => o.Id == request.OrgUnitId, cancellationToken)
            ?? throw new NotFoundException(nameof(OrgUnit), request.OrgUnitId);

        if (request.IsActive)
        {
            orgUnit.Activate();
        }
        else
        {
            orgUnit.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
