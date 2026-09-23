using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Templates;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Templates;

public sealed record GetTemplatesQuery(
    int PageNumber = 1,
    int PageSize = 50,
    /// <summary>name (default) | key | latestVersionNumber | isActive</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<TemplateSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Templates.Read;
}

public sealed record TemplateSummary(Guid Id, string Key, string Name, bool IsActive, int LatestVersionNumber);

public sealed class GetTemplatesQueryHandler(IApplicationDbContext db) : IRequestHandler<GetTemplatesQuery, PagedResult<TemplateSummary>>
{
    public async Task<PagedResult<TemplateSummary>> Handle(GetTemplatesQuery request, CancellationToken cancellationToken)
    {
        // LatestVersionNumber depende de una propiedad calculada en memoria (LatestVersion, sobre la
        // colección Versions ya cargada) — no es traducible a SQL, así que el orden/paginado por esa
        // columna se resuelve en memoria; el resto se resuelve en la base de datos como de costumbre.
        var templates = await db.Templates.AsNoTracking().Include(t => t.Versions).ToListAsync(cancellationToken);

        var descending = request.SortDescending;
        IEnumerable<Template> ordered = request.SortBy switch
        {
            "key" => descending ? templates.OrderByDescending(t => t.Key) : templates.OrderBy(t => t.Key),
            "latestVersionNumber" => descending
                ? templates.OrderByDescending(t => t.LatestVersion?.VersionNumber ?? 0)
                : templates.OrderBy(t => t.LatestVersion?.VersionNumber ?? 0),
            "isActive" => descending ? templates.OrderByDescending(t => t.IsActive) : templates.OrderBy(t => t.IsActive),
            "name" => descending ? templates.OrderByDescending(t => t.Name) : templates.OrderBy(t => t.Name),
            _ => templates.OrderBy(t => t.Name),
        };

        var summaries = ordered
            .Select(t => new TemplateSummary(t.Id, t.Key, t.Name, t.IsActive, t.LatestVersion?.VersionNumber ?? 0))
            .ToList();

        // PagedResult.CreateAsync espera un IQueryable respaldado por EF (usa CountAsync/ToListAsync de
        // EF Core) — summaries ya es una lista en memoria, así que se pagina a mano en vez de forzar un
        // IQueryable falso con AsQueryable() (que fallaría en tiempo de ejecución al no ser una query real).
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;
        var page = summaries.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<TemplateSummary>(page, summaries.Count, pageNumber, pageSize);
    }
}
