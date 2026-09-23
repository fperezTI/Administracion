using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Organization.Companies;

public sealed record GetCompaniesQuery(
    int PageNumber = 1,
    int PageSize = 50,
    bool? IsActive = null,
    /// <summary>tradeName (default) | legalName | taxId | baseCurrency | timeZone | isActive</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<CompanySummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Companies.Read;
}

public sealed record CompanySummary(
    Guid Id, string LegalName, string TradeName, string TaxId, string BaseCurrency, string TimeZone, bool IsActive);

public sealed class GetCompaniesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCompaniesQuery, PagedResult<CompanySummary>>
{
    public Task<PagedResult<CompanySummary>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var query = db.Companies.AsNoTracking().AsQueryable();

        if (request.IsActive is { } isActive)
        {
            query = query.Where(c => c.IsActive == isActive);
        }

        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "legalName" => descending ? query.OrderByDescending(c => c.LegalName) : query.OrderBy(c => c.LegalName),
            "taxId" => descending ? query.OrderByDescending(c => c.TaxId) : query.OrderBy(c => c.TaxId),
            "baseCurrency" => descending ? query.OrderByDescending(c => c.BaseCurrency) : query.OrderBy(c => c.BaseCurrency),
            "timeZone" => descending ? query.OrderByDescending(c => c.TimeZone) : query.OrderBy(c => c.TimeZone),
            "isActive" => descending ? query.OrderByDescending(c => c.IsActive) : query.OrderBy(c => c.IsActive),
            "tradeName" => descending ? query.OrderByDescending(c => c.TradeName) : query.OrderBy(c => c.TradeName),
            _ => query.OrderBy(c => c.TradeName),
        };

        var projected = ordered.Select(c => new CompanySummary(
            c.Id, c.LegalName, c.TradeName, c.TaxId, c.BaseCurrency, c.TimeZone, c.IsActive));

        return PagedResult<CompanySummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
