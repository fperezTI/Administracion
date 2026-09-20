using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Organization.Companies;

public sealed record GetCompanyByIdQuery(Guid CompanyId) : IRequest<CompanySummary>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Companies.Read;
}

public sealed class GetCompanyByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetCompanyByIdQuery, CompanySummary>
{
    public async Task<CompanySummary> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var company = await db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        return new CompanySummary(
            company.Id, company.LegalName, company.TradeName, company.TaxId,
            company.BaseCurrency, company.TimeZone, company.IsActive);
    }
}
