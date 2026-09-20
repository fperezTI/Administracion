using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Roles;

/// <summary>Replaces a role's entire permission matrix — this is the "Permisos" module action
/// (distinct from "Roles"), see docs/security/authorization-rbac.md.</summary>
public sealed record SetRolePermissionsCommand(Guid RoleId, IReadOnlyCollection<Guid> PermissionIds)
    : IRequest, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Permissions.Manage;
}

public sealed class SetRolePermissionsCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SetRolePermissionsCommand>
{
    public async Task Handle(SetRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        var validPermissionIds = await db.Permissions
            .Where(p => request.PermissionIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        if (validPermissionIds.Count != request.PermissionIds.Distinct().Count())
        {
            throw new ConflictException("Uno o más identificadores de permiso no existen en el catálogo.");
        }

        role.SetPermissions(validPermissionIds);
        await db.SaveChangesAsync(cancellationToken);
    }
}
