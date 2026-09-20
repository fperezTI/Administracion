using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Templates;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Templates;

public sealed record GetTemplateByIdQuery(Guid TemplateId) : IRequest<TemplateDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Templates.Read;
}

public sealed record TemplateVersionInfo(int VersionNumber, string Content, DateTimeOffset CreatedAtUtc);

public sealed record TemplateDetail(Guid Id, string Key, string Name, bool IsActive, IReadOnlyList<TemplateVersionInfo> Versions);

public sealed class GetTemplateByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetTemplateByIdQuery, TemplateDetail>
{
    public async Task<TemplateDetail> Handle(GetTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var template = await db.Templates.AsNoTracking().Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken)
            ?? throw new NotFoundException(nameof(Template), request.TemplateId);

        return new TemplateDetail(
            template.Id, template.Key, template.Name, template.IsActive,
            template.Versions.OrderByDescending(v => v.VersionNumber)
                .Select(v => new TemplateVersionInfo(v.VersionNumber, v.Content, v.CreatedAtUtc))
                .ToList());
    }
}
