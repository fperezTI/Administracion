using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Organization.OrgUnits;

/// <summary>Reorganizes the tree. The tree-wide "no cycles" invariant spans multiple OrgUnit
/// instances, so it is verified here rather than inside the OrgUnit aggregate — see OrgUnit.cs.</summary>
public sealed record MoveOrgUnitCommand(Guid OrgUnitId, Guid? NewParentOrgUnitId) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Structure.Update;
}

public sealed class MoveOrgUnitCommandHandler(IApplicationDbContext db) : IRequestHandler<MoveOrgUnitCommand>
{
    public async Task Handle(MoveOrgUnitCommand request, CancellationToken cancellationToken)
    {
        var orgUnit = await db.OrgUnits.FirstOrDefaultAsync(o => o.Id == request.OrgUnitId, cancellationToken)
            ?? throw new NotFoundException(nameof(OrgUnit), request.OrgUnitId);

        if (request.NewParentOrgUnitId is { } newParentId)
        {
            var newParent = await db.OrgUnits.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == newParentId, cancellationToken)
                ?? throw new NotFoundException(nameof(OrgUnit), newParentId);

            if (newParent.CompanyId != orgUnit.CompanyId)
            {
                throw new ConflictException("La nueva unidad padre debe pertenecer a la misma empresa.");
            }

            await EnsureNoCycleAsync(db, orgUnit.Id, newParentId, cancellationToken);
        }

        orgUnit.MoveTo(request.NewParentOrgUnitId);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureNoCycleAsync(
        IApplicationDbContext db, Guid movingId, Guid newParentId, CancellationToken cancellationToken)
    {
        var currentId = (Guid?)newParentId;
        var guard = 0;

        while (currentId is { } id)
        {
            if (id == movingId)
            {
                throw new ConflictException(
                    "El movimiento crearía un ciclo: la nueva unidad padre es descendiente de la unidad que se mueve.");
            }

            if (++guard > 1000)
            {
                throw new ConflictException("La jerarquía organizacional es demasiado profunda para validarse.");
            }

            currentId = await db.OrgUnits.AsNoTracking()
                .Where(o => o.Id == id)
                .Select(o => o.ParentOrgUnitId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
