using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Users;

public sealed record AssignRoleToUserCommand(Guid UserId, Guid RoleId) : IRequest, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Users.ManageRoles;
}

public sealed class AssignRoleToUserCommandHandler(IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<AssignRoleToUserCommand>
{
    public async Task Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var roleExists = await db.Roles.AnyAsync(r => r.Id == request.RoleId && r.IsActive, cancellationToken);
        if (!roleExists)
        {
            throw new NotFoundException(nameof(Role), request.RoleId);
        }

        user.AssignRole(request.RoleId, currentUser.UserId, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }
}
