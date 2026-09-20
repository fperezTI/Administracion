using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Organization;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.SparePartsAndConsumables;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.SparePartsAndConsumables;

public sealed record UninstallSparePartCommand(Guid SparePartId, Guid WarehouseOrgUnitId) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.SpareParts.Update;
}

public sealed class UninstallSparePartCommandValidator : AbstractValidator<UninstallSparePartCommand>
{
    public UninstallSparePartCommandValidator()
    {
        RuleFor(x => x.SparePartId).NotEmpty();
        RuleFor(x => x.WarehouseOrgUnitId).NotEmpty();
    }
}

public sealed class UninstallSparePartCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<UninstallSparePartCommand>
{
    public async Task Handle(UninstallSparePartCommand request, CancellationToken cancellationToken)
    {
        var sparePart = await db.SpareParts.Include(p => p.Installations)
            .FirstOrDefaultAsync(p => p.Id == request.SparePartId, cancellationToken)
            ?? throw new NotFoundException(nameof(SparePart), request.SparePartId);

        if (!currentCompany.AccessibleCompanyIds.Contains(sparePart.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de esta refacción.");
        }

        var warehouse = await (
            from ou in db.OrgUnits
            join type in db.OrgUnitTypes on ou.OrgUnitTypeId equals type.Id
            where ou.Id == request.WarehouseOrgUnitId
            select new { ou.CompanyId, type.Code }).FirstOrDefaultAsync(cancellationToken);

        if (warehouse is null || warehouse.CompanyId != sparePart.CompanyId || warehouse.Code != OrgUnitTypeCatalog.Codes.Warehouse)
        {
            throw new ConflictException("El almacén indicado no es válido para esta empresa.");
        }

        sparePart.Uninstall(request.WarehouseOrgUnitId, clock.UtcNow, currentUser.UserId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
