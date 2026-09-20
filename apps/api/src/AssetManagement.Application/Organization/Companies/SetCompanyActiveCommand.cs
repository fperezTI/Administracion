using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Organization.Companies;

public sealed record SetCompanyActiveCommand(Guid CompanyId, bool IsActive) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Companies.Update;
}

public sealed class SetCompanyActiveCommandHandler(IApplicationDbContext db) : IRequestHandler<SetCompanyActiveCommand>
{
    public async Task Handle(SetCompanyActiveCommand request, CancellationToken cancellationToken)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        if (request.IsActive)
        {
            company.Activate();
        }
        else
        {
            company.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
