using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.SparePartsAndConsumables;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.SparePartsAndConsumables;

public sealed record GetConsumableStockMovementsQuery(Guid ConsumableId)
    : IRequest<IReadOnlyList<ConsumableStockMovementSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Consumables.Read;
}

public sealed record ConsumableStockMovementSummary(
    Guid Id, string Folio, Guid WarehouseOrgUnitId, ConsumableStockDirection Direction, ConsumableStockMovementReason Reason,
    decimal Quantity, Guid? ReferenceMaintenanceOrderId, string? Notes, DateTimeOffset OccurredAtUtc);

public sealed class GetConsumableStockMovementsQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetConsumableStockMovementsQuery, IReadOnlyList<ConsumableStockMovementSummary>>
{
    public async Task<IReadOnlyList<ConsumableStockMovementSummary>> Handle(
        GetConsumableStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var consumable = await db.Consumables.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ConsumableId, cancellationToken)
            ?? throw new NotFoundException(nameof(Consumable), request.ConsumableId);

        if (!currentCompany.AccessibleCompanyIds.Contains(consumable.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este consumible.");
        }

        return await db.ConsumableStockMovements.AsNoTracking().Where(m => m.ConsumableId == request.ConsumableId)
            .OrderByDescending(m => m.OccurredAtUtc)
            .Select(m => new ConsumableStockMovementSummary(
                m.Id, m.Folio, m.WarehouseOrgUnitId, m.Direction, m.Reason, m.Quantity, m.ReferenceMaintenanceOrderId,
                m.Notes, m.OccurredAtUtc))
            .ToListAsync(cancellationToken);
    }
}
