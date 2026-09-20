using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets.Categories;

public sealed record GetAssetCategoryByIdQuery(Guid AssetCategoryId) : IRequest<AssetCategoryDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Catalogs.Read;
}

public sealed record CustomFieldDefinitionSummary(
    Guid Id, string Name, string Code, CustomFieldDataType DataType, bool IsRequired, int SortOrder, string? Options);

public sealed record AssetCategoryDetail(
    Guid Id,
    string Name,
    string Code,
    bool IsActive,
    IdentificationTechnology DefaultIdentificationTechnology,
    IReadOnlyCollection<CustomFieldDefinitionSummary> CustomFields);

public sealed class GetAssetCategoryByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAssetCategoryByIdQuery, AssetCategoryDetail>
{
    public async Task<AssetCategoryDetail> Handle(GetAssetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await db.AssetCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.AssetCategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(AssetCategory), request.AssetCategoryId);

        var fields = await db.CustomFieldDefinitions.AsNoTracking()
            .Where(f => f.AssetCategoryId == request.AssetCategoryId)
            .OrderBy(f => f.SortOrder)
            .Select(f => new CustomFieldDefinitionSummary(f.Id, f.Name, f.Code, f.DataType, f.IsRequired, f.SortOrder, f.Options))
            .ToListAsync(cancellationToken);

        return new AssetCategoryDetail(
            category.Id, category.Name, category.Code, category.IsActive, category.DefaultIdentificationTechnology, fields);
    }
}
