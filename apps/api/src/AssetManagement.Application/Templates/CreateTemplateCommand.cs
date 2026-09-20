using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Templates;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Templates;

public sealed record CreateTemplateCommand(string Key, string Name, string InitialContent) : IRequest<Guid>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Templates.Create;
}

public sealed class CreateTemplateCommandValidator : AbstractValidator<CreateTemplateCommand>
{
    public CreateTemplateCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InitialContent).NotEmpty().MaximumLength(10000);
    }
}

public sealed class CreateTemplateCommandHandler(IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<CreateTemplateCommand, Guid>
{
    public async Task<Guid> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
    {
        var keyTaken = await db.Templates.AnyAsync(t => t.Key == request.Key, cancellationToken);
        if (keyTaken)
        {
            throw new ConflictException("Ya existe una plantilla con esa clave.");
        }

        var now = clock.UtcNow;
        var template = Template.Create(request.Key, request.Name, request.InitialContent, now, currentUser.UserId);

        db.Templates.Add(template);
        await db.SaveChangesAsync(cancellationToken);

        return template.Id;
    }
}
