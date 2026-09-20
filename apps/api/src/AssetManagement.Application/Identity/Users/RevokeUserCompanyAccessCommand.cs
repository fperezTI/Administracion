using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Users;

public sealed record RevokeUserCompanyAccessCommand(Guid UserId, Guid CompanyId) : IRequest, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Users.ManageCompanies;
}

public sealed class RevokeUserCompanyAccessCommandHandler(IApplicationDbContext db)
    : IRequestHandler<RevokeUserCompanyAccessCommand>
{
    public async Task Handle(RevokeUserCompanyAccessCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.Include(u => u.UserCompanies)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        user.RevokeCompanyAccess(request.CompanyId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
