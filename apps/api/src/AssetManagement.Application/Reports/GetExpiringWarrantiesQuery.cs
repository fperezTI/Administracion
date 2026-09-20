using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Maintenance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Reports;

/// <summary>Warranties ending within <c>WithinDays</c> from today, or already ended — both are actionable
/// and shown together, ordered soonest first (see ADR 0012, decision 2).</summary>
public sealed record GetExpiringWarrantiesQuery(Guid? CompanyId, int WithinDays = 30) : IRequest<IReadOnlyList<ExpiringWarrantyRow>>, IRequiresPermission
{
    public string PermissionCode => ReportScope.PermissionCode(CompanyId);
}

public sealed record ExpiringWarrantyRow(
    Guid WarrantyId, Guid AssetId, string AssetFolio, string Provider, WarrantyType Type, DateOnly EndDate, int DaysRemaining);

public sealed class GetExpiringWarrantiesQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany, IClock clock)
    : IRequestHandler<GetExpiringWarrantiesQuery, IReadOnlyList<ExpiringWarrantyRow>>
{
    public Task<IReadOnlyList<ExpiringWarrantyRow>> Handle(GetExpiringWarrantiesQuery request, CancellationToken cancellationToken) =>
        FetchAsync(db, currentCompany, clock, request.CompanyId, request.WithinDays, cancellationToken);

    /// <summary>Shared with <see cref="ExportExpiringWarrantiesQueryHandler"/> so both read the exact same rows.</summary>
    internal static async Task<IReadOnlyList<ExpiringWarrantyRow>> FetchAsync(
        IApplicationDbContext db, ICurrentCompanyContext currentCompany, IClock clock, Guid? companyId, int withinDays,
        CancellationToken cancellationToken)
    {
        const int maxRows = 1000;

        var companyIds = ReportScope.ResolveCompanyIds(companyId, currentCompany);
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var threshold = today.AddDays(Math.Max(0, withinDays));

        var rows = await (
            from w in db.Warranties.AsNoTracking()
            where companyIds.Contains(w.CompanyId) && w.EndDate <= threshold
            join a in db.Assets.AsNoTracking() on w.AssetId equals a.Id
            orderby w.EndDate
            select new { w.Id, w.AssetId, a.InternalFolio, w.Provider, w.Type, w.EndDate })
            .Take(maxRows)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new ExpiringWarrantyRow(r.Id, r.AssetId, r.InternalFolio, r.Provider, r.Type, r.EndDate, r.EndDate.DayNumber - today.DayNumber))
            .ToList();
    }
}
