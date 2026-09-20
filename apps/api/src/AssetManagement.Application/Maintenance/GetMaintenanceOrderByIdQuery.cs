using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record GetMaintenanceOrderByIdQuery(Guid MaintenanceOrderId) : IRequest<MaintenanceOrderDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Maintenance.Read;
}

public sealed record MaintenanceOrderChecklistResultInfo(int ItemIndex, string ItemText, bool IsCompleted, string? Notes);

public sealed record MaintenanceOrderDetail(
    Guid Id, Guid CompanyId, Guid AssetId, string AssetFolio, string Folio, MaintenanceOrderType Type,
    MaintenanceOrderStatus Status, string Description, Guid? ChecklistDefinitionId, int? ChecklistVersionNumber,
    IReadOnlyList<MaintenanceOrderChecklistResultInfo> ChecklistResults, DateTimeOffset OpenedAtUtc,
    DateTimeOffset? ClosedAtUtc, AssetStatus? ResultStatus, string? ResultNotes);

public sealed class GetMaintenanceOrderByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMaintenanceOrderByIdQuery, MaintenanceOrderDetail>
{
    public async Task<MaintenanceOrderDetail> Handle(GetMaintenanceOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await db.MaintenanceOrders.AsNoTracking().Include(o => o.ChecklistResults)
            .FirstOrDefaultAsync(o => o.Id == request.MaintenanceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(MaintenanceOrder), request.MaintenanceOrderId);

        var asset = await db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == order.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), order.AssetId);

        return new MaintenanceOrderDetail(
            order.Id, order.CompanyId, order.AssetId, asset.InternalFolio, order.Folio, order.Type, order.Status,
            order.Description, order.ChecklistDefinitionId, order.ChecklistVersionNumber,
            order.ChecklistResults.OrderBy(r => r.ItemIndex)
                .Select(r => new MaintenanceOrderChecklistResultInfo(r.ItemIndex, r.ItemText, r.IsCompleted, r.Notes))
                .ToList(),
            order.OpenedAtUtc, order.ClosedAtUtc, order.ResultStatus, order.ResultNotes);
    }
}
