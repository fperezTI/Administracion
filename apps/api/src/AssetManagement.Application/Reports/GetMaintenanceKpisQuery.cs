using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Maintenance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Reports;

/// <summary>MTTR (horas, promedio de <c>ClosedAtUtc - OpenedAtUtc</c> sobre órdenes cerradas) y MTBF
/// (días, promedio agrupado de los intervalos entre <c>OpenedAtUtc</c> consecutivos de un mismo activo con
/// 2+ órdenes) — ver ADR 0012. Calculado en memoria sobre las órdenes ya materializadas, mismo criterio de
/// volumen que <c>GetMyPendingApprovalsQuery</c> (ADR 0006) acepta para V1.</summary>
public sealed record GetMaintenanceKpisQuery(Guid? CompanyId) : IRequest<MaintenanceKpisResult>, IRequiresPermission
{
    public string PermissionCode => ReportScope.PermissionCode(CompanyId);
}

public sealed record MaintenanceCategoryKpis(
    Guid AssetCategoryId, string AssetCategoryName, double? MttrHours, double? MtbfDays, int ClosedOrdersCount);

public sealed record MaintenanceKpisResult(
    double? MttrHours, double? MtbfDays, int ClosedOrdersCount, IReadOnlyList<MaintenanceCategoryKpis> ByCategory);

public sealed class GetMaintenanceKpisQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetMaintenanceKpisQuery, MaintenanceKpisResult>
{
    private sealed record OrderRow(Guid AssetId, Guid AssetCategoryId, string AssetCategoryName, DateTimeOffset OpenedAtUtc, DateTimeOffset? ClosedAtUtc, MaintenanceOrderStatus Status);

    public Task<MaintenanceKpisResult> Handle(GetMaintenanceKpisQuery request, CancellationToken cancellationToken) =>
        FetchAsync(db, currentCompany, request.CompanyId, cancellationToken);

    /// <summary>Shared with <see cref="ExportMaintenanceKpisQueryHandler"/> so both read the exact same rows.</summary>
    internal static async Task<MaintenanceKpisResult> FetchAsync(
        IApplicationDbContext db, ICurrentCompanyContext currentCompany, Guid? companyId, CancellationToken cancellationToken)
    {
        var companyIds = ReportScope.ResolveCompanyIds(companyId, currentCompany);

        // Selects the raw Status enum rather than comparing it to a constant inside the projection —
        // the SQL Server provider translates a boolean equality over an enum mapped to nvarchar
        // (HasConversion<string>()) into an invalid '^' operator between nvarchar operands. Same root
        // cause GetMyAssignmentsQuery hit in F3; the fix there was the same: never embed that comparison
        // in a query translated to SQL, compare in memory instead.
        var orders = await (
            from o in db.MaintenanceOrders.AsNoTracking()
            where companyIds.Contains(o.CompanyId)
            join a in db.Assets.AsNoTracking() on o.AssetId equals a.Id
            join c in db.AssetCategories.AsNoTracking() on a.AssetCategoryId equals c.Id
            select new OrderRow(o.AssetId, c.Id, c.Name, o.OpenedAtUtc, o.ClosedAtUtc, o.Status))
            .ToListAsync(cancellationToken);

        var (overallMttr, overallMtbf, closedCount) = ComputeKpis(orders);

        var byCategory = orders
            .GroupBy(o => new { o.AssetCategoryId, o.AssetCategoryName })
            .Select(g =>
            {
                var (mttr, mtbf, closed) = ComputeKpis(g.ToList());
                return new MaintenanceCategoryKpis(g.Key.AssetCategoryId, g.Key.AssetCategoryName, mttr, mtbf, closed);
            })
            .OrderByDescending(x => x.ClosedOrdersCount)
            .ToList();

        return new MaintenanceKpisResult(overallMttr, overallMtbf, closedCount, byCategory);
    }

    private static (double? MttrHours, double? MtbfDays, int ClosedCount) ComputeKpis(IReadOnlyList<OrderRow> orders)
    {
        var closedDurations = orders
            .Where(o => o.Status == MaintenanceOrderStatus.Closed && o.ClosedAtUtc is not null)
            .Select(o => (o.ClosedAtUtc!.Value - o.OpenedAtUtc).TotalHours)
            .ToList();
        double? mttr = closedDurations.Count == 0 ? null : closedDurations.Average();

        var gaps = orders
            .GroupBy(o => o.AssetId)
            .SelectMany(g =>
            {
                var opened = g.Select(o => o.OpenedAtUtc).OrderBy(d => d).ToList();
                var assetGaps = new List<double>();
                for (var i = 1; i < opened.Count; i++)
                {
                    assetGaps.Add((opened[i] - opened[i - 1]).TotalDays);
                }

                return assetGaps;
            })
            .ToList();
        double? mtbf = gaps.Count == 0 ? null : gaps.Average();

        return (mttr, mtbf, closedDurations.Count);
    }
}
