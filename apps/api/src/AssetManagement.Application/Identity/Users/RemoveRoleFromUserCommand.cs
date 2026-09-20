using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Users;

public sealed record RemoveRoleFromUserCommand(Guid UserId, Guid RoleId) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Users.ManageRoles;
}

public sealed class RemoveRoleFromUserCommandHandler(IApplicationDbContext db) : IRequestHandler<RemoveRoleFromUserCommand>
{
    public async Task Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.RemoveRole(request.RoleId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
