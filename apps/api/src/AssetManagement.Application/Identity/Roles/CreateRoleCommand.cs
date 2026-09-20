using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using FluentValidation;
using MediatR;

namespace AssetManagement.Application.Identity.Roles;

public sealed record CreateRoleCommand(string Name, string? Description) : IRequest<Guid>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Roles.Create;
}

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class CreateRoleCommandHandler(IApplicationDbContext db, IClock clock) : IRequestHandler<CreateRoleCommand, Guid>
{
    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = Role.Create(request.Name, request.Description, clock.UtcNow);
        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);
        return role.Id;
    }
}
