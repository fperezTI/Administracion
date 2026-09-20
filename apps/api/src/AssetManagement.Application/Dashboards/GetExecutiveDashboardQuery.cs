using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Application.Reports;
using AssetManagement.Domain.Approvals;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Dashboards;

/// <summary>Single-screen operational snapshot, gated by its own permission (<see
/// cref="PermissionCatalog.Dashboards.ViewExecutive"/>) independent of <c>Reports.*</c> so it can be
/// handed to someone without giving them the full Reports module. Reuses <see cref="ReportScope"/> for
/// company-scope resolution only — not its permission code.</summary>
public sealed record GetExecutiveDashboardQuery(Guid? CompanyId) : IRequest<ExecutiveDashboardResult>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Dashboards.ViewExecutive;
}

public sealed record ExecutiveDashboardResult(
    int TotalAssets,
    int AssignedAssets,
    int AvailableAssets,
    int PendingApprovals,
    int OpenMaintenanceOrders,
    int ExpiringWarranties,
    int LowStockConsumables);

public sealed class GetExecutiveDashboardQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany, IClock clock)
    : IRequestHandler<GetExecutiveDashboardQuery, ExecutiveDashboardResult>
{
    public async Task<ExecutiveDashboardResult> Handle(GetExecutiveDashboardQuery request, CancellationToken cancellationToken)
    {
        var companyIds = ReportScope.ResolveCompanyIds(request.CompanyId, currentCompany);

        // Status columns are all HasConversion<string>() — SQL Server can't translate an equality
        // filter over them (see GetMaintenanceKpisQuery), so every status is selected raw and counted
        // in memory, same as the rest of Reports.
        var assetStatuses = await db.Assets.AsNoTracking()
            .Where(a => companyIds.Contains(a.CompanyId))
            .Select(a => a.Status)
            .ToListAsync(cancellationToken);

        var approvalStatuses = await db.ApprovalInstances.AsNoTracking()
            .Where(a => companyIds.Contains(a.CompanyId))
            .Select(a => a.Status)
            .ToListAsync(cancellationToken);

        var maintenanceStatuses = await db.MaintenanceOrders.AsNoTracking()
            .Where(o => companyIds.Contains(o.CompanyId))
            .Select(o => o.Status)
            .ToListAsync(cancellationToken);

        var expiringWarranties = await GetExpiringWarrantiesQueryHandler.FetchAsync(
            db, currentCompany, clock, request.CompanyId, withinDays: 30, cancellationToken);

        var lowStockConsumables = await GetLowStockConsumablesQueryHandler.FetchAsync(
            db, currentCompany, request.CompanyId, cancellationToken);

        return new ExecutiveDashboardResult(
            TotalAssets: assetStatuses.Count,
            AssignedAssets: assetStatuses.Count(s => s is AssetStatus.Assigned or AssetStatus.OnLoan),
            AvailableAssets: assetStatuses.Count(s => s == AssetStatus.InWarehouse),
            PendingApprovals: approvalStatuses.Count(s => s == ApprovalInstanceStatus.Pending),
            OpenMaintenanceOrders: maintenanceStatuses.Count(s => s == MaintenanceOrderStatus.Open),
            ExpiringWarranties: expiringWarranties.Count,
            LowStockConsumables: lowStockConsumables.Count);
    }
}
