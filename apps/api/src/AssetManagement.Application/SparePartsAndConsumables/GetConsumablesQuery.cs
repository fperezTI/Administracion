using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.SparePartsAndConsumables;

public sealed record GetConsumablesQuery(Guid CompanyId) : IRequest<IReadOnlyList<ConsumableSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Consumables.Read;
}

public sealed record ConsumableSummary(
    Guid Id, string Name, string? Sku, string UnitOfMeasure, decimal? MinimumStock, decimal CurrentStock);

public sealed class GetConsumablesQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetConsumablesQuery, IReadOnlyList<ConsumableSummary>>
{
    public async Task<IReadOnlyList<ConsumableSummary>> Handle(GetConsumablesQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        return await db.Consumables.AsNoTracking().Where(c => c.CompanyId == request.CompanyId).OrderBy(c => c.Name)
            .Select(c => new ConsumableSummary(c.Id, c.Name, c.Sku, c.UnitOfMeasure, c.MinimumStock, c.CurrentStock))
            .ToListAsync(cancellationToken);
    }
}
