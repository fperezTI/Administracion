using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Templates;

public sealed record GetTemplatesQuery : IRequest<IReadOnlyList<TemplateSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Templates.Read;
}

public sealed record TemplateSummary(Guid Id, string Key, string Name, bool IsActive, int LatestVersionNumber);

public sealed class GetTemplatesQueryHandler(IApplicationDbContext db) : IRequestHandler<GetTemplatesQuery, IReadOnlyList<TemplateSummary>>
{
    public async Task<IReadOnlyList<TemplateSummary>> Handle(GetTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await db.Templates.AsNoTracking().Include(t => t.Versions).OrderBy(t => t.Name).ToListAsync(cancellationToken);

        return templates
            .Select(t => new TemplateSummary(t.Id, t.Key, t.Name, t.IsActive, t.LatestVersion?.VersionNumber ?? 0))
            .ToList();
    }
}
