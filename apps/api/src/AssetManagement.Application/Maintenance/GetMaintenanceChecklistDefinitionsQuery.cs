using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Maintenance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record GetMaintenanceChecklistDefinitionsQuery(
    int PageNumber = 1,
    int PageSize = 50,
    /// <summary>name (default) | key | latestVersionNumber | isActive</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<MaintenanceChecklistDefinitionSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Maintenance.Read;
}

public sealed record MaintenanceChecklistDefinitionSummary(
    Guid Id, string Key, string Name, Guid? AssetCategoryId, bool IsActive, int LatestVersionNumber);

public sealed class GetMaintenanceChecklistDefinitionsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMaintenanceChecklistDefinitionsQuery, PagedResult<MaintenanceChecklistDefinitionSummary>>
{
    public async Task<PagedResult<MaintenanceChecklistDefinitionSummary>> Handle(
        GetMaintenanceChecklistDefinitionsQuery request, CancellationToken cancellationToken)
    {
        // LatestVersionNumber es una propiedad calculada en memoria (igual que en GetTemplatesQuery) —
        // el orden/paginado se resuelve en memoria, no es traducible a SQL.
        var checklists = await db.MaintenanceChecklistDefinitions.AsNoTracking().Include(c => c.Versions)
            .ToListAsync(cancellationToken);

        var descending = request.SortDescending;
        IEnumerable<MaintenanceChecklistDefinition> ordered = request.SortBy switch
        {
            "key" => descending ? checklists.OrderByDescending(c => c.Key) : checklists.OrderBy(c => c.Key),
            "latestVersionNumber" => descending
                ? checklists.OrderByDescending(c => c.LatestVersion?.VersionNumber ?? 0)
                : checklists.OrderBy(c => c.LatestVersion?.VersionNumber ?? 0),
            "isActive" => descending ? checklists.OrderByDescending(c => c.IsActive) : checklists.OrderBy(c => c.IsActive),
            "name" => descending ? checklists.OrderByDescending(c => c.Name) : checklists.OrderBy(c => c.Name),
            _ => checklists.OrderBy(c => c.Name),
        };

        var summaries = ordered
            .Select(c => new MaintenanceChecklistDefinitionSummary(
                c.Id, c.Key, c.Name, c.AssetCategoryId, c.IsActive, c.LatestVersion?.VersionNumber ?? 0))
            .ToList();

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;
        var page = summaries.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<MaintenanceChecklistDefinitionSummary>(page, summaries.Count, pageNumber, pageSize);
    }
}
