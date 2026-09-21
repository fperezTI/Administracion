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
    string? Search = null)
    : IRequest<PagedResult<AssetSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assets.Read;
}

public sealed record AssetSummary(
    Guid Id,
    string InternalFolio,
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

        var projected = query.OrderBy(a => a.InternalFolio).Select(a => new AssetSummary(
            a.Id, a.InternalFolio, a.AssetCategoryId, a.Brand, a.Model, a.SerialNumber, a.Status, a.PhysicalCondition,
            a.AccessoryOfAssetId));

        return PagedResult<AssetSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
