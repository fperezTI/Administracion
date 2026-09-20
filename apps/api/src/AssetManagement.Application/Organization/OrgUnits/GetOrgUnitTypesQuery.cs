using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Organization.OrgUnits;

public sealed record GetOrgUnitTypesQuery : IRequest<IReadOnlyCollection<OrgUnitTypeSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Structure.Read;
}

public sealed record OrgUnitTypeSummary(Guid Id, string Code, string Name, bool IsActive);

public sealed class GetOrgUnitTypesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetOrgUnitTypesQuery, IReadOnlyCollection<OrgUnitTypeSummary>>
{
    public async Task<IReadOnlyCollection<OrgUnitTypeSummary>> Handle(
        GetOrgUnitTypesQuery request, CancellationToken cancellationToken)
    {
        return await db.OrgUnitTypes.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new OrgUnitTypeSummary(t.Id, t.Code, t.Name, t.IsActive))
            .ToListAsync(cancellationToken);
    }
}
