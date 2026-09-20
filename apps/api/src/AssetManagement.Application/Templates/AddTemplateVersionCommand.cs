using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Templates;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Templates;

public sealed record AddTemplateVersionCommand(Guid TemplateId, string Content) : IRequest<int>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Templates.Update;
}

public sealed class AddTemplateVersionCommandValidator : AbstractValidator<AddTemplateVersionCommand>
{
    public AddTemplateVersionCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(10000);
    }
}

public sealed class AddTemplateVersionCommandHandler(IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<AddTemplateVersionCommand, int>
{
    public async Task<int> Handle(AddTemplateVersionCommand request, CancellationToken cancellationToken)
    {
        var template = await db.Templates.Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken)
            ?? throw new NotFoundException(nameof(Template), request.TemplateId);

        var version = template.AddVersion(request.Content, clock.UtcNow, currentUser.UserId);
        await db.SaveChangesAsync(cancellationToken);

        return version.VersionNumber;
    }
}
