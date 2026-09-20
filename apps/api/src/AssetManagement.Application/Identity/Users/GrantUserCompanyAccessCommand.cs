using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Users;

public sealed record GrantUserCompanyAccessCommand(Guid UserId, Guid CompanyId) : IRequest, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Users.ManageCompanies;
}

public sealed class GrantUserCompanyAccessCommandHandler(IApplicationDbContext db, IClock clock)
    : IRequestHandler<GrantUserCompanyAccessCommand>
{
    public async Task Handle(GrantUserCompanyAccessCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.Include(u => u.UserCompanies)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var companyExists = await db.Companies.AnyAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (!companyExists)
        {
            throw new NotFoundException(nameof(Company), request.CompanyId);
        }

        user.GrantCompanyAccess(request.CompanyId, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }
}
