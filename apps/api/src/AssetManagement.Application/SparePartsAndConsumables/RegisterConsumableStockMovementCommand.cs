using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Organization;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Maintenance;
using AssetManagement.Domain.SparePartsAndConsumables;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.SparePartsAndConsumables;

/// <summary>Registers one change in a consumable's existence — the only way <see cref="Consumable.CurrentStock"/>
/// changes (pedido: "todo cambio de existencia genera un movimiento de inventario de consumible"). Creates
/// the immutable <see cref="ConsumableStockMovement"/> row and applies the same delta to the consumable in
/// one transaction (one <c>SaveChangesAsync</c>).</summary>
public sealed record RegisterConsumableStockMovementCommand(
    Guid ConsumableId, Guid WarehouseOrgUnitId, ConsumableStockDirection Direction, ConsumableStockMovementReason Reason,
    decimal Quantity, Guid? ReferenceMaintenanceOrderId, string? Notes)
    : IRequest<Guid>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Consumables.Update;
}

public sealed class RegisterConsumableStockMovementCommandValidator : AbstractValidator<RegisterConsumableStockMovementCommand>
{
    public RegisterConsumableStockMovementCommandValidator()
    {
        RuleFor(x => x.ConsumableId).NotEmpty();
        RuleFor(x => x.WarehouseOrgUnitId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class RegisterConsumableStockMovementCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IFolioGenerator folioGenerator, IClock clock)
    : IRequestHandler<RegisterConsumableStockMovementCommand, Guid>
{
    public async Task<Guid> Handle(RegisterConsumableStockMovementCommand request, CancellationToken cancellationToken)
    {
        var consumable = await db.Consumables.FirstOrDefaultAsync(c => c.Id == request.ConsumableId, cancellationToken)
            ?? throw new NotFoundException(nameof(Consumable), request.ConsumableId);

        if (!currentCompany.AccessibleCompanyIds.Contains(consumable.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este consumible.");
        }

        var warehouse = await (
            from ou in db.OrgUnits
            join type in db.OrgUnitTypes on ou.OrgUnitTypeId equals type.Id
            where ou.Id == request.WarehouseOrgUnitId
            select new { ou.CompanyId, type.Code }).FirstOrDefaultAsync(cancellationToken);

        if (warehouse is null || warehouse.CompanyId != consumable.CompanyId || warehouse.Code != OrgUnitTypeCatalog.Codes.Warehouse)
        {
            throw new ConflictException("El almacén indicado no es válido para esta empresa.");
        }

        if (request.ReferenceMaintenanceOrderId is { } orderId)
        {
            var orderExists = await db.MaintenanceOrders.AnyAsync(o => o.Id == orderId, cancellationToken);
            if (!orderExists)
            {
                throw new NotFoundException(nameof(MaintenanceOrder), orderId);
            }
        }

        var now = clock.UtcNow;
        var folio = await folioGenerator.NextAsync(consumable.CompanyId, FolioDocumentTypes.ConsumableStockMovement, cancellationToken);

        var movement = ConsumableStockMovement.Create(
            consumable.CompanyId, consumable.Id, folio, request.WarehouseOrgUnitId, request.Direction, request.Reason,
            request.Quantity, request.ReferenceMaintenanceOrderId, request.Notes, now, currentUser.UserId);

        consumable.ApplyStockMovement(request.Direction, request.Quantity, now, currentUser.UserId);

        db.ConsumableStockMovements.Add(movement);
        await db.SaveChangesAsync(cancellationToken);

        return movement.Id;
    }
}
