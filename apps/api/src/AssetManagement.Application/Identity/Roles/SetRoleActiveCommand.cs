using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Roles;

/// <summary>Roles are never physically deleted (pedido §6) — only deactivated, which this command does.</summary>
public sealed record SetRoleActiveCommand(Guid RoleId, bool IsActive) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Roles.Update;
}

public sealed class SetRoleActiveCommandHandler(IApplicationDbContext db) : IRequestHandler<SetRoleActiveCommand>
{
    public async Task Handle(SetRoleActiveCommand request, CancellationToken cancellationToken)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        if (request.IsActive)
        {
            role.Activate();
        }
        else
        {
            role.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
