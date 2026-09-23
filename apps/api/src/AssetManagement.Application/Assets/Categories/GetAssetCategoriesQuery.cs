using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets.Categories;

public sealed record GetAssetCategoriesQuery(
    int PageNumber = 1,
    int PageSize = 50,
    bool? IsActive = null,
    /// <summary>name (default) | code | defaultIdentificationTechnology | customFieldCount | isActive</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<AssetCategorySummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Catalogs.Read;
}

public sealed record AssetCategorySummary(
    Guid Id, string Name, string Code, bool IsActive, IdentificationTechnology DefaultIdentificationTechnology, int CustomFieldCount);

public sealed class GetAssetCategoriesQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAssetCategoriesQuery, PagedResult<AssetCategorySummary>>
{
    public Task<PagedResult<AssetCategorySummary>> Handle(GetAssetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var query = db.AssetCategories.AsNoTracking().AsQueryable();

        if (request.IsActive is { } isActive)
        {
            query = query.Where(c => c.IsActive == isActive);
        }

        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "code" => descending ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
            "defaultIdentificationTechnology" => descending
                ? query.OrderByDescending(c => c.DefaultIdentificationTechnology)
                : query.OrderBy(c => c.DefaultIdentificationTechnology),
            "customFieldCount" => descending
                ? query.OrderByDescending(c => c.CustomFields.Count)
                : query.OrderBy(c => c.CustomFields.Count),
            "isActive" => descending ? query.OrderByDescending(c => c.IsActive) : query.OrderBy(c => c.IsActive),
            "name" => descending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            _ => query.OrderBy(c => c.Name),
        };

        var projected = ordered.Select(c => new AssetCategorySummary(
            c.Id, c.Name, c.Code, c.IsActive, c.DefaultIdentificationTechnology, c.CustomFields.Count));

        return PagedResult<AssetCategorySummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
