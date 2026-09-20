using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Organization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Organization.OrgUnits;

public sealed record CreateOrgUnitCommand(
    Guid CompanyId, Guid OrgUnitTypeId, Guid? ParentOrgUnitId, string Name, string Code)
    : IRequest<Guid>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Structure.Create;
}

public sealed class CreateOrgUnitCommandValidator : AbstractValidator<CreateOrgUnitCommand>
{
    public CreateOrgUnitCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.OrgUnitTypeId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateOrgUnitCommandHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany, IClock clock)
    : IRequestHandler<CreateOrgUnitCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrgUnitCommand request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var orgUnitTypeExists = await db.OrgUnitTypes
            .AnyAsync(t => t.Id == request.OrgUnitTypeId && t.IsActive, cancellationToken);
        if (!orgUnitTypeExists)
        {
            throw new NotFoundException(nameof(OrgUnitType), request.OrgUnitTypeId);
        }

        if (request.ParentOrgUnitId is { } parentId)
        {
            var parentExistsInCompany = await db.OrgUnits
                .AnyAsync(o => o.Id == parentId && o.CompanyId == request.CompanyId, cancellationToken);
            if (!parentExistsInCompany)
            {
                throw new NotFoundException(nameof(OrgUnit), parentId);
            }
        }

        var orgUnit = OrgUnit.Create(
            request.CompanyId, request.OrgUnitTypeId, request.ParentOrgUnitId, request.Name, request.Code, clock.UtcNow);

        db.OrgUnits.Add(orgUnit);
        await db.SaveChangesAsync(cancellationToken);

        return orgUnit.Id;
    }
}
