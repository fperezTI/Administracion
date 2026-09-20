using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

/// <summary>
/// Opens a maintenance order (pedido: "preventivo/correctivo"). Moves the asset to
/// <see cref="AssetStatus.InMaintenance"/> — the only entry point that does, mirroring how
/// <see cref="Inventory.RequestCrossCompanyTransferCommand"/> is the only entry point into
/// <see cref="AssetStatus.InTransit"/>. Linking a checklist is optional; when given, the checklist's
/// current latest version's items are snapshotted onto the order (see the F6 plan, decision 4).
/// </summary>
public sealed record OpenMaintenanceOrderCommand(
    Guid AssetId, MaintenanceOrderType Type, string Description, Guid? ChecklistDefinitionId)
    : IRequest<Guid>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Maintenance.Create;
}

public sealed class OpenMaintenanceOrderCommandValidator : AbstractValidator<OpenMaintenanceOrderCommand>
{
    public OpenMaintenanceOrderCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
    }
}

public sealed class OpenMaintenanceOrderCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IFolioGenerator folioGenerator, IClock clock)
    : IRequestHandler<OpenMaintenanceOrderCommand, Guid>
{
    public async Task<Guid> Handle(OpenMaintenanceOrderCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        if (!currentCompany.AccessibleCompanyIds.Contains(asset.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este activo.");
        }

        if (asset.Status != AssetStatus.InWarehouse && asset.Status != AssetStatus.Assigned)
        {
            throw new ConflictException("Solo un activo en almacén o asignado puede enviarse a mantenimiento.");
        }

        Guid? checklistDefinitionId = null;
        int? checklistVersionNumber = null;
        IReadOnlyList<string>? checklistItems = null;
        if (request.ChecklistDefinitionId is { } checklistId)
        {
            var checklist = await db.MaintenanceChecklistDefinitions.Include(c => c.Versions)
                .FirstOrDefaultAsync(c => c.Id == checklistId, cancellationToken)
                ?? throw new NotFoundException(nameof(MaintenanceChecklistDefinition), checklistId);

            if (checklist.LatestVersion is null)
            {
                throw new ConflictException("Este checklist todavía no tiene ninguna versión.");
            }

            checklistDefinitionId = checklist.Id;
            checklistVersionNumber = checklist.LatestVersion.VersionNumber;
            checklistItems = checklist.LatestVersion.Items;
        }

        var now = clock.UtcNow;
        var folio = await folioGenerator.NextAsync(asset.CompanyId, FolioDocumentTypes.MaintenanceOrder, cancellationToken);

        var order = MaintenanceOrder.Open(
            asset.CompanyId, asset.Id, folio, request.Type, request.Description, checklistDefinitionId,
            checklistVersionNumber, checklistItems, now, currentUser.UserId);

        asset.ChangeStatus(AssetStatus.InMaintenance, now, currentUser.UserId);

        db.MaintenanceOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
