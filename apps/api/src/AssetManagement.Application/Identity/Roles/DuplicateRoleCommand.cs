using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Roles;

public sealed record DuplicateRoleCommand(Guid SourceRoleId, string NewName) : IRequest<Guid>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Roles.Duplicate;
}

public sealed class DuplicateRoleCommandValidator : AbstractValidator<DuplicateRoleCommand>
{
    public DuplicateRoleCommandValidator()
    {
        RuleFor(x => x.NewName).NotEmpty().MaximumLength(100);
    }
}

public sealed class DuplicateRoleCommandHandler(IApplicationDbContext db, IClock clock)
    : IRequestHandler<DuplicateRoleCommand, Guid>
{
    public async Task<Guid> Handle(DuplicateRoleCommand request, CancellationToken cancellationToken)
    {
        var source = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.SourceRoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.SourceRoleId);

        var copy = source.Duplicate(request.NewName, clock.UtcNow);

        db.Roles.Add(copy);
        await db.SaveChangesAsync(cancellationToken);

        return copy.Id;
    }
}
