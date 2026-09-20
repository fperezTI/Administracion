using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Approvals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Approvals;

public sealed record GetApprovalFlowDefinitionsQuery : IRequest<IReadOnlyList<ApprovalFlowDefinitionSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Approvals.Read;
}

public sealed record ApprovalFlowDefinitionSummary(
    Guid Id,
    string Key,
    Guid? CompanyId,
    IReadOnlyList<ApproverRoleInfo> ApproverRoles,
    int RequiredApprovals,
    ApprovalMode Mode,
    bool RequiresComment,
    bool IsActive);

public sealed record ApproverRoleInfo(Guid RoleId, string RoleName);

public sealed class GetApprovalFlowDefinitionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetApprovalFlowDefinitionsQuery, IReadOnlyList<ApprovalFlowDefinitionSummary>>
{
    public async Task<IReadOnlyList<ApprovalFlowDefinitionSummary>> Handle(
        GetApprovalFlowDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var flows = await db.ApprovalFlowDefinitions.AsNoTracking()
            .OrderBy(f => f.Key)
            .ToListAsync(cancellationToken);

        var roleNamesById = await db.Roles.AsNoTracking().ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        return flows.Select(f => new ApprovalFlowDefinitionSummary(
            f.Id, f.Key, f.CompanyId,
            f.ApproverRoleIds.Select(id => new ApproverRoleInfo(id, roleNamesById.GetValueOrDefault(id, "(rol eliminado)"))).ToList(),
            f.RequiredApprovals, f.Mode, f.RequiresComment, f.IsActive)).ToList();
    }
}
