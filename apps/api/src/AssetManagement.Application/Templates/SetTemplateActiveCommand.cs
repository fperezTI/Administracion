using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Templates;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Templates;

public sealed record SetTemplateActiveCommand(Guid TemplateId, bool IsActive) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Templates.Update;
}

public sealed class SetTemplateActiveCommandHandler(IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<SetTemplateActiveCommand>
{
    public async Task Handle(SetTemplateActiveCommand request, CancellationToken cancellationToken)
    {
        var template = await db.Templates.FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken)
            ?? throw new NotFoundException(nameof(Template), request.TemplateId);

        template.SetActive(request.IsActive, clock.UtcNow, currentUser.UserId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
