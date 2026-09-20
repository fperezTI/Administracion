using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Organization.Companies;

public sealed record GetCompaniesQuery(int PageNumber = 1, int PageSize = 50, bool? IsActive = null)
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
        var query = db.Companies.AsNoTracking().OrderBy(c => c.TradeName).AsQueryable();

        if (request.IsActive is { } isActive)
        {
            query = query.Where(c => c.IsActive == isActive);
        }

        var projected = query.Select(c => new CompanySummary(
            c.Id, c.LegalName, c.TradeName, c.TaxId, c.BaseCurrency, c.TimeZone, c.IsActive));

        return PagedResult<CompanySummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
