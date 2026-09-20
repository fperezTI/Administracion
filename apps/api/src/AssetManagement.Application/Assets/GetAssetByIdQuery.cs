using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets;

public sealed record GetAssetByIdQuery(Guid AssetId) : IRequest<AssetDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assets.Read;
}

public sealed record AssetTagInfo(string Code, IdentificationTechnology Technology, int PrintCount);

public sealed record AssetCustomFieldValueInfo(Guid CustomFieldDefinitionId, string Value);

public sealed record AssetDetail(
    Guid Id,
    Guid CompanyId,
    Guid AssetCategoryId,
    string InternalFolio,
    string? PatrimonialFolio,
    string Brand,
    string Model,
    string? SerialNumber,
    string? Description,
    AssetStatus Status,
    PhysicalCondition PhysicalCondition,
    Guid? CurrentOrgUnitId,
    DateOnly? AcquisitionDate,
    decimal? AcquisitionCost,
    string? Currency,
    string? Supplier,
    string? Invoice,
    string? PurchaseOrder,
    DateOnly? WarrantyStartDate,
    DateOnly? WarrantyEndDate,
    string? SupportContract,
    string? SupportProvider,
    AssetTagInfo? Tag,
    IReadOnlyCollection<AssetCustomFieldValueInfo> CustomFieldValues,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed class GetAssetByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetAssetByIdQuery, AssetDetail>
{
    public async Task<AssetDetail> Handle(GetAssetByIdQuery request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.AsNoTracking()
            .Include(a => a.Tag)
            .Include(a => a.CustomFieldValues)
            .FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        return new AssetDetail(
            asset.Id, asset.CompanyId, asset.AssetCategoryId, asset.InternalFolio, asset.PatrimonialFolio,
            asset.Brand, asset.Model, asset.SerialNumber, asset.Description, asset.Status, asset.PhysicalCondition,
            asset.CurrentOrgUnitId, asset.AcquisitionDate, asset.AcquisitionCost, asset.Currency, asset.Supplier,
            asset.Invoice, asset.PurchaseOrder, asset.WarrantyStartDate, asset.WarrantyEndDate, asset.SupportContract,
            asset.SupportProvider,
            asset.Tag is null ? null : new AssetTagInfo(asset.Tag.Code, asset.Tag.Technology, asset.Tag.PrintCount),
            asset.CustomFieldValues.Select(v => new AssetCustomFieldValueInfo(v.CustomFieldDefinitionId, v.Value)).ToList(),
            asset.CreatedAtUtc, asset.UpdatedAtUtc);
    }
}
