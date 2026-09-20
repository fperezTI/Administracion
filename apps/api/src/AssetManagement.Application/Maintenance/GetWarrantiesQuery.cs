using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Maintenance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record GetWarrantiesQuery(Guid CompanyId, Guid? AssetId = null)
    : IRequest<IReadOnlyList<WarrantySummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Warranties.Read;
}

public sealed record WarrantySummary(
    Guid Id, Guid AssetId, string AssetFolio, WarrantyType Type, string Provider, DateOnly StartDate,
    DateOnly EndDate, string? Terms);

public sealed class GetWarrantiesQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetWarrantiesQuery, IReadOnlyList<WarrantySummary>>
{
    public async Task<IReadOnlyList<WarrantySummary>> Handle(GetWarrantiesQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var query = db.Warranties.AsNoTracking().Where(w => w.CompanyId == request.CompanyId);

        if (request.AssetId is { } assetId)
        {
            query = query.Where(w => w.AssetId == assetId);
        }

        var projected =
            from w in query
            join asset in db.Assets.AsNoTracking() on w.AssetId equals asset.Id
            orderby w.EndDate descending
            select new WarrantySummary(w.Id, w.AssetId, asset.InternalFolio, w.Type, w.Provider, w.StartDate, w.EndDate, w.Terms);

        return await projected.ToListAsync(cancellationToken);
    }
}
