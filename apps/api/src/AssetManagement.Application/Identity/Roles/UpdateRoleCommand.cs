using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Roles;

public sealed record UpdateRoleCommand(Guid RoleId, string Name, string? Description) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Roles.Update;
}

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class UpdateRoleCommandHandler(IApplicationDbContext db) : IRequestHandler<UpdateRoleCommand>
{
    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        role.Rename(request.Name, request.Description);
        await db.SaveChangesAsync(cancellationToken);
    }
}
