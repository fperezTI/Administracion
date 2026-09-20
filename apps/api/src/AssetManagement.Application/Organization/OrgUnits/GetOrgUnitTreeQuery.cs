using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Organization.OrgUnits;

/// <summary>Returns the flat list of org units for a company; the client composes the tree from
/// ParentOrgUnitId. A company's org tree is small enough (pedido §31: up to 100 warehouses plus other
/// unit kinds) that a flat list beats a materialized-path optimization for V1 — see ADR 0002.</summary>
public sealed record GetOrgUnitTreeQuery(Guid CompanyId) : IRequest<IReadOnlyCollection<OrgUnitNode>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Structure.Read;
}

public sealed record OrgUnitNode(
    Guid Id, Guid? ParentOrgUnitId, Guid OrgUnitTypeId, string OrgUnitTypeName, string Name, string Code, bool IsActive);

public sealed class GetOrgUnitTreeQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetOrgUnitTreeQuery, IReadOnlyCollection<OrgUnitNode>>
{
    public async Task<IReadOnlyCollection<OrgUnitNode>> Handle(
        GetOrgUnitTreeQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var nodes = await (
            from orgUnit in db.OrgUnits.AsNoTracking()
            join type in db.OrgUnitTypes.AsNoTracking() on orgUnit.OrgUnitTypeId equals type.Id
            where orgUnit.CompanyId == request.CompanyId
            orderby orgUnit.Name
            select new OrgUnitNode(
                orgUnit.Id, orgUnit.ParentOrgUnitId, orgUnit.OrgUnitTypeId, type.Name,
                orgUnit.Name, orgUnit.Code, orgUnit.IsActive))
            .ToListAsync(cancellationToken);

        return nodes;
    }
}
