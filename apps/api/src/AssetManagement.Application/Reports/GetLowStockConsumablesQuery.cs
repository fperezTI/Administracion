using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Reports;

/// <summary>Consumables with a defined minimum whose current stock has reached or fallen below it —
/// consumables without a minimum defined are excluded (there is nothing to compare against), see ADR
/// 0012.</summary>
public sealed record GetLowStockConsumablesQuery(Guid? CompanyId) : IRequest<IReadOnlyList<LowStockConsumableRow>>, IRequiresPermission
{
    public string PermissionCode => ReportScope.PermissionCode(CompanyId);
}

public sealed record LowStockConsumableRow(
    Guid ConsumableId, string Name, string? Sku, string UnitOfMeasure, decimal CurrentStock, decimal MinimumStock, decimal Shortfall);

public sealed class GetLowStockConsumablesQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetLowStockConsumablesQuery, IReadOnlyList<LowStockConsumableRow>>
{
    public Task<IReadOnlyList<LowStockConsumableRow>> Handle(GetLowStockConsumablesQuery request, CancellationToken cancellationToken) =>
        FetchAsync(db, currentCompany, request.CompanyId, cancellationToken);

    /// <summary>Shared with <see cref="ExportLowStockConsumablesQueryHandler"/> so both read the exact same rows.</summary>
    internal static async Task<IReadOnlyList<LowStockConsumableRow>> FetchAsync(
        IApplicationDbContext db, ICurrentCompanyContext currentCompany, Guid? companyId, CancellationToken cancellationToken)
    {
        const int maxRows = 1000;
        var companyIds = ReportScope.ResolveCompanyIds(companyId, currentCompany);

        // Ordered before projecting into the record — the SQL Server provider can fail to translate
        // OrderBy over a property of an already-projected custom record type (and separately,
        // `c.MinimumStock!.Value` doesn't translate even though the Where above guarantees non-null;
        // `?? 0` does, via ISNULL/COALESCE).
        return await db.Consumables.AsNoTracking()
            .Where(c => companyIds.Contains(c.CompanyId) && c.MinimumStock != null && c.CurrentStock <= c.MinimumStock)
            .OrderByDescending(c => (c.MinimumStock ?? 0) - c.CurrentStock)
            .Take(maxRows)
            .Select(c => new LowStockConsumableRow(
                c.Id, c.Name, c.Sku, c.UnitOfMeasure, c.CurrentStock, c.MinimumStock ?? 0, (c.MinimumStock ?? 0) - c.CurrentStock))
            .ToListAsync(cancellationToken);
    }
}
