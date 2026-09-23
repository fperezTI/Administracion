using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets;

public sealed record GetAssetsQuery(
    Guid CompanyId,
    int PageNumber = 1,
    int PageSize = 50,
    Guid? AssetCategoryId = null,
    AssetStatus? Status = null,
    string? Search = null,
    /// <summary>internalFolio (default) | description | category | brand | serialNumber | physicalCondition | status</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<AssetSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assets.Read;
}

public sealed record AssetSummary(
    Guid Id,
    string InternalFolio,
    string? Description,
    Guid AssetCategoryId,
    string Brand,
    string Model,
    string? SerialNumber,
    AssetStatus Status,
    PhysicalCondition PhysicalCondition,
    Guid? AccessoryOfAssetId);

public sealed class GetAssetsQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetAssetsQuery, PagedResult<AssetSummary>>
{
    public Task<PagedResult<AssetSummary>> Handle(GetAssetsQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var query = db.Assets.AsNoTracking().Where(a => a.CompanyId == request.CompanyId);

        if (request.AssetCategoryId is { } categoryId)
        {
            query = query.Where(a => a.AssetCategoryId == categoryId);
        }

        if (request.Status is { } status)
        {
            query = query.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(a =>
                a.InternalFolio.Contains(term) ||
                a.Brand.Contains(term) ||
                a.Model.Contains(term) ||
                (a.SerialNumber != null && a.SerialNumber.Contains(term)));
        }

        var joined =
            from a in query
            join c in db.AssetCategories.AsNoTracking() on a.AssetCategoryId equals c.Id
            select new { Asset = a, CategoryName = c.Name };

        // Description/SerialNumber son nullable: sin el OrderBy(...== null) inicial, SQL Server pone los
        // NULL primero en ASC — así quedan siempre al final, sin importar la dirección.
        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "description" => descending
                ? joined.OrderBy(x => x.Asset.Description == null).ThenByDescending(x => x.Asset.Description)
                : joined.OrderBy(x => x.Asset.Description == null).ThenBy(x => x.Asset.Description),
            "category" => descending
                ? joined.OrderByDescending(x => x.CategoryName)
                : joined.OrderBy(x => x.CategoryName),
            "brand" => descending
                ? joined.OrderByDescending(x => x.Asset.Brand).ThenByDescending(x => x.Asset.Model)
                : joined.OrderBy(x => x.Asset.Brand).ThenBy(x => x.Asset.Model),
            "serialNumber" => descending
                ? joined.OrderBy(x => x.Asset.SerialNumber == null).ThenByDescending(x => x.Asset.SerialNumber)
                : joined.OrderBy(x => x.Asset.SerialNumber == null).ThenBy(x => x.Asset.SerialNumber),
            "physicalCondition" => descending
                ? joined.OrderByDescending(x => x.Asset.PhysicalCondition)
                : joined.OrderBy(x => x.Asset.PhysicalCondition),
            "status" => descending
                ? joined.OrderByDescending(x => x.Asset.Status)
                : joined.OrderBy(x => x.Asset.Status),
            _ => descending
                ? joined.OrderByDescending(x => x.Asset.InternalFolio)
                : joined.OrderBy(x => x.Asset.InternalFolio),
        };

        var projected = ordered.Select(x => new AssetSummary(
            x.Asset.Id, x.Asset.InternalFolio, x.Asset.Description, x.Asset.AssetCategoryId, x.Asset.Brand,
            x.Asset.Model, x.Asset.SerialNumber, x.Asset.Status, x.Asset.PhysicalCondition,
            x.Asset.AccessoryOfAssetId));

        return PagedResult<AssetSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
