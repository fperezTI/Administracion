using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Approvals;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Approvals;

public sealed record GetApprovalFlowDefinitionsQuery(
    int PageNumber = 1,
    int PageSize = 50,
    /// <summary>key (default) | companyId (alcance) | mode | requiredApprovals | isActive — ApproverRoles
    /// no es ordenable: es una lista, no un valor escalar.</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<ApprovalFlowDefinitionSummary>>, IRequiresPermission
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
    : IRequestHandler<GetApprovalFlowDefinitionsQuery, PagedResult<ApprovalFlowDefinitionSummary>>
{
    public async Task<PagedResult<ApprovalFlowDefinitionSummary>> Handle(
        GetApprovalFlowDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var query = db.ApprovalFlowDefinitions.AsNoTracking();

        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "companyId" => descending
                ? query.OrderByDescending(f => f.CompanyId != null)
                : query.OrderBy(f => f.CompanyId != null),
            "mode" => descending ? query.OrderByDescending(f => f.Mode) : query.OrderBy(f => f.Mode),
            "requiredApprovals" => descending
                ? query.OrderByDescending(f => f.RequiredApprovals)
                : query.OrderBy(f => f.RequiredApprovals),
            "isActive" => descending ? query.OrderByDescending(f => f.IsActive) : query.OrderBy(f => f.IsActive),
            "key" => descending ? query.OrderByDescending(f => f.Key) : query.OrderBy(f => f.Key),
            _ => query.OrderBy(f => f.Key),
        };

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;

        var totalCount = await query.CountAsync(cancellationToken);
        var pageFlows = await ordered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        // Solo se consultan los roles que realmente aparecen en esta página, no el catálogo completo.
        var roleIds = pageFlows.SelectMany(f => f.ApproverRoleIds).Distinct().ToList();
        var roleNamesById = await db.Roles.AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        var summaries = pageFlows.Select(f => new ApprovalFlowDefinitionSummary(
            f.Id, f.Key, f.CompanyId,
            f.ApproverRoleIds.Select(id => new ApproverRoleInfo(id, roleNamesById.GetValueOrDefault(id, "(rol eliminado)"))).ToList(),
            f.RequiredApprovals, f.Mode, f.RequiresComment, f.IsActive)).ToList();

        return new PagedResult<ApprovalFlowDefinitionSummary>(summaries, totalCount, pageNumber, pageSize);
    }
}
