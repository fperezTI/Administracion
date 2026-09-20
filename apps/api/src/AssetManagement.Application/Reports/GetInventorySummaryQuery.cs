using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Reports;

/// <summary>Landing view of the operational dashboard — asset counts by status and by category. No
/// export: it's orientation, not a worklist (see ADR 0012, decision 2).</summary>
public sealed record GetInventorySummaryQuery(Guid? CompanyId) : IRequest<InventorySummaryResult>, IRequiresPermission
{
    public string PermissionCode => ReportScope.PermissionCode(CompanyId);
}

public sealed record AssetStatusCount(AssetStatus Status, int Count);

public sealed record AssetCategoryCount(Guid AssetCategoryId, string AssetCategoryName, int Count);

public sealed record InventorySummaryResult(int TotalAssets, IReadOnlyList<AssetStatusCount> ByStatus, IReadOnlyList<AssetCategoryCount> ByCategory);

public sealed class GetInventorySummaryQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetInventorySummaryQuery, InventorySummaryResult>
{
    public async Task<InventorySummaryResult> Handle(GetInventorySummaryQuery request, CancellationToken cancellationToken)
    {
        var companyIds = ReportScope.ResolveCompanyIds(request.CompanyId, currentCompany);

        // Grouped/counted in memory rather than translated server-side — same "materialize, then
        // aggregate" precedent GetMyPendingApprovalsQuery already set (ADR 0006), and it sidesteps
        // EF Core InMemory provider's stricter GroupBy-translation limits (SQL Server itself handles the
        // server-side shape fine, but this keeps unit and integration tests exercising identical logic).
        var assets = await db.Assets.AsNoTracking()
            .Where(a => companyIds.Contains(a.CompanyId))
            .Select(a => new { a.Status, a.AssetCategoryId })
            .ToListAsync(cancellationToken);

        var categoryNamesById = await db.AssetCategories.AsNoTracking()
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var byStatus = assets
            .GroupBy(a => a.Status)
            .Select(g => new AssetStatusCount(g.Key, g.Count()))
            .OrderBy(x => x.Status)
            .ToList();

        var byCategory = assets
            .GroupBy(a => a.AssetCategoryId)
            .Select(g => new AssetCategoryCount(g.Key, categoryNamesById.GetValueOrDefault(g.Key, "—"), g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        return new InventorySummaryResult(assets.Count, byStatus, byCategory);
    }
}
