using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record GetMaintenanceChecklistDefinitionsQuery
    : IRequest<IReadOnlyList<MaintenanceChecklistDefinitionSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Maintenance.Read;
}

public sealed record MaintenanceChecklistDefinitionSummary(
    Guid Id, string Key, string Name, Guid? AssetCategoryId, bool IsActive, int LatestVersionNumber);

public sealed class GetMaintenanceChecklistDefinitionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMaintenanceChecklistDefinitionsQuery, IReadOnlyList<MaintenanceChecklistDefinitionSummary>>
{
    public async Task<IReadOnlyList<MaintenanceChecklistDefinitionSummary>> Handle(
        GetMaintenanceChecklistDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var checklists = await db.MaintenanceChecklistDefinitions.AsNoTracking().Include(c => c.Versions)
            .OrderBy(c => c.Name).ToListAsync(cancellationToken);

        return checklists
            .Select(c => new MaintenanceChecklistDefinitionSummary(
                c.Id, c.Key, c.Name, c.AssetCategoryId, c.IsActive, c.LatestVersion?.VersionNumber ?? 0))
            .ToList();
    }
}
