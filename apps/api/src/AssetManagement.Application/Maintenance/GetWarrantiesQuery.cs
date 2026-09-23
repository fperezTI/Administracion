using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Maintenance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record GetWarrantiesQuery(
    Guid CompanyId,
    Guid? AssetId = null,
    int PageNumber = 1,
    int PageSize = 50,
    /// <summary>endDate descendente (default) | assetFolio | type | provider</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<WarrantySummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Warranties.Read;
}

public sealed record WarrantySummary(
    Guid Id, Guid AssetId, string AssetFolio, WarrantyType Type, string Provider, DateOnly StartDate,
    DateOnly EndDate, string? Terms);

public sealed class GetWarrantiesQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetWarrantiesQuery, PagedResult<WarrantySummary>>
{
    public Task<PagedResult<WarrantySummary>> Handle(GetWarrantiesQuery request, CancellationToken cancellationToken)
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

        var joined =
            from w in query
            join asset in db.Assets.AsNoTracking() on w.AssetId equals asset.Id
            select new { Warranty = w, AssetFolio = asset.InternalFolio };

        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "assetFolio" => descending ? joined.OrderByDescending(x => x.AssetFolio) : joined.OrderBy(x => x.AssetFolio),
            "type" => descending ? joined.OrderByDescending(x => x.Warranty.Type) : joined.OrderBy(x => x.Warranty.Type),
            "provider" => descending ? joined.OrderByDescending(x => x.Warranty.Provider) : joined.OrderBy(x => x.Warranty.Provider),
            "endDate" => descending ? joined.OrderByDescending(x => x.Warranty.EndDate) : joined.OrderBy(x => x.Warranty.EndDate),
            // Default histórico (sin SortBy): la que vence más tarde primero, sin importar SortDescending.
            _ => joined.OrderByDescending(x => x.Warranty.EndDate),
        };

        var projected = ordered.Select(x => new WarrantySummary(
            x.Warranty.Id, x.Warranty.AssetId, x.AssetFolio, x.Warranty.Type, x.Warranty.Provider,
            x.Warranty.StartDate, x.Warranty.EndDate, x.Warranty.Terms));

        return PagedResult<WarrantySummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
