using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets.Categories;

public sealed record GetAssetCategoriesQuery(int PageNumber = 1, int PageSize = 50, bool? IsActive = null)
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
        var query = db.AssetCategories.AsNoTracking().OrderBy(c => c.Name).AsQueryable();

        if (request.IsActive is { } isActive)
        {
            query = query.Where(c => c.IsActive == isActive);
        }

        var projected = query.Select(c => new AssetCategorySummary(
            c.Id, c.Name, c.Code, c.IsActive, c.DefaultIdentificationTechnology, c.CustomFields.Count));

        return PagedResult<AssetCategorySummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
