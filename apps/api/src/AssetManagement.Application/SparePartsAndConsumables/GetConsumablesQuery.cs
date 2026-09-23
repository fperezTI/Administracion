using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.SparePartsAndConsumables;

public sealed record GetConsumablesQuery(
    Guid CompanyId,
    int PageNumber = 1,
    int PageSize = 50,
    /// <summary>name (default) | sku | currentStock | minimumStock</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<ConsumableSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Consumables.Read;
}

public sealed record ConsumableSummary(
    Guid Id, string Name, string? Sku, string UnitOfMeasure, decimal? MinimumStock, decimal CurrentStock);

public sealed class GetConsumablesQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetConsumablesQuery, PagedResult<ConsumableSummary>>
{
    public Task<PagedResult<ConsumableSummary>> Handle(GetConsumablesQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var query = db.Consumables.AsNoTracking().Where(c => c.CompanyId == request.CompanyId);

        // Sku/MinimumStock son nullable: se ordenan siempre al final sin importar la dirección.
        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "sku" => descending
                ? query.OrderBy(c => c.Sku == null).ThenByDescending(c => c.Sku)
                : query.OrderBy(c => c.Sku == null).ThenBy(c => c.Sku),
            "currentStock" => descending ? query.OrderByDescending(c => c.CurrentStock) : query.OrderBy(c => c.CurrentStock),
            "minimumStock" => descending
                ? query.OrderBy(c => c.MinimumStock == null).ThenByDescending(c => c.MinimumStock)
                : query.OrderBy(c => c.MinimumStock == null).ThenBy(c => c.MinimumStock),
            "name" => descending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            _ => query.OrderBy(c => c.Name),
        };

        var projected = ordered.Select(c => new ConsumableSummary(c.Id, c.Name, c.Sku, c.UnitOfMeasure, c.MinimumStock, c.CurrentStock));

        return PagedResult<ConsumableSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
