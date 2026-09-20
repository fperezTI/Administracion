using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Roles;

public sealed record GetRoleByIdQuery(Guid RoleId) : IRequest<RoleDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Roles.Read;
}

public sealed record RoleDetail(Guid Id, string Name, string? Description, bool IsActive, IReadOnlyCollection<Guid> PermissionIds);

public sealed class GetRoleByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetRoleByIdQuery, RoleDetail>
{
    public async Task<RoleDetail> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        var permissionIds = await db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == request.RoleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        return new RoleDetail(role.Id, role.Name, role.Description, role.IsActive, permissionIds);
    }
}
