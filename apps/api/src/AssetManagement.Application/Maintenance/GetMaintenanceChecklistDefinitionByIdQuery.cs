using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Maintenance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record GetMaintenanceChecklistDefinitionByIdQuery(Guid ChecklistDefinitionId)
    : IRequest<MaintenanceChecklistDefinitionDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Maintenance.Read;
}

public sealed record MaintenanceChecklistVersionInfo(int VersionNumber, IReadOnlyList<string> Items, DateTimeOffset CreatedAtUtc);

public sealed record MaintenanceChecklistDefinitionDetail(
    Guid Id, string Key, string Name, Guid? AssetCategoryId, bool IsActive, IReadOnlyList<MaintenanceChecklistVersionInfo> Versions);

public sealed class GetMaintenanceChecklistDefinitionByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMaintenanceChecklistDefinitionByIdQuery, MaintenanceChecklistDefinitionDetail>
{
    public async Task<MaintenanceChecklistDefinitionDetail> Handle(
        GetMaintenanceChecklistDefinitionByIdQuery request, CancellationToken cancellationToken)
    {
        var checklist = await db.MaintenanceChecklistDefinitions.AsNoTracking().Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.Id == request.ChecklistDefinitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(MaintenanceChecklistDefinition), request.ChecklistDefinitionId);

        return new MaintenanceChecklistDefinitionDetail(
            checklist.Id, checklist.Key, checklist.Name, checklist.AssetCategoryId, checklist.IsActive,
            checklist.Versions.OrderByDescending(v => v.VersionNumber)
                .Select(v => new MaintenanceChecklistVersionInfo(v.VersionNumber, v.Items, v.CreatedAtUtc))
                .ToList());
    }
}
