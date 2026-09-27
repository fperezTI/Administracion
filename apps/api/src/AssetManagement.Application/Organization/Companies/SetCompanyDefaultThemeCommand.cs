using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Organization;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Organization.Companies;

/// <summary>Changes a company's default theme — every user of that company who hasn't set a personal
/// preference sees this. Gated by Companies.Update (pedido: "solo el Super Administrador"; this system
/// has no role-name check, only permission codes — the bootstrap Super Administrador role holds every
/// permission, this one included, same as it already gates SetCompanyActiveCommand).</summary>
public sealed record SetCompanyDefaultThemeCommand(Guid CompanyId, string ThemeCode) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Companies.Update;
}

public sealed class SetCompanyDefaultThemeCommandHandler(IApplicationDbContext db)
    : IRequestHandler<SetCompanyDefaultThemeCommand>
{
    public async Task Handle(SetCompanyDefaultThemeCommand request, CancellationToken cancellationToken)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        company.SetDefaultTheme(request.ThemeCode);

        await db.SaveChangesAsync(cancellationToken);
    }
}
