using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record MaintenanceOrderChecklistItemResultInput(int ItemIndex, bool IsCompleted, string? Notes);

/// <summary>Closes a maintenance order. <see cref="ResultStatus"/> is restricted by
/// <see cref="MaintenanceOrder.Close"/> to <see cref="AssetStatus.InWarehouse"/> or
/// <see cref="AssetStatus.Damaged"/> — see the F6 plan, decision 3, for why decommission is deliberately
/// excluded here.</summary>
public sealed record CloseMaintenanceOrderCommand(
    Guid MaintenanceOrderId, AssetStatus ResultStatus, string ResultNotes,
    IReadOnlyList<MaintenanceOrderChecklistItemResultInput>? ChecklistItemResults)
    : IRequest, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Maintenance.Update;
}

public sealed class CloseMaintenanceOrderCommandValidator : AbstractValidator<CloseMaintenanceOrderCommand>
{
    public CloseMaintenanceOrderCommandValidator()
    {
        RuleFor(x => x.MaintenanceOrderId).NotEmpty();
        RuleFor(x => x.ResultNotes).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ResultStatus).Must(s => s == AssetStatus.InWarehouse || s == AssetStatus.Damaged)
            .WithMessage("El resultado solo puede ser 'En almacén' o 'Dañado'.");
    }
}

public sealed class CloseMaintenanceOrderCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<CloseMaintenanceOrderCommand>
{
    public async Task Handle(CloseMaintenanceOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await db.MaintenanceOrders.Include(o => o.ChecklistResults)
            .FirstOrDefaultAsync(o => o.Id == request.MaintenanceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(MaintenanceOrder), request.MaintenanceOrderId);

        if (!currentCompany.AccessibleCompanyIds.Contains(order.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de esta orden de mantenimiento.");
        }

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == order.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), order.AssetId);

        var checklistUpdates = request.ChecklistItemResults?
            .ToDictionary(r => r.ItemIndex, r => (r.IsCompleted, r.Notes));

        var now = clock.UtcNow;
        order.Close(request.ResultStatus, request.ResultNotes, checklistUpdates, now, currentUser.UserId);
        asset.ChangeStatus(request.ResultStatus, now, currentUser.UserId);

        await db.SaveChangesAsync(cancellationToken);
    }
}
