using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Organization;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.SparePartsAndConsumables;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.SparePartsAndConsumables;

public sealed record CreateSparePartCommand(
    Guid CompanyId, string Name, string? PartNumber, string SerialNumber, Guid WarehouseOrgUnitId)
    : IRequest<Guid>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.SpareParts.Create;
}

public sealed class CreateSparePartCommandValidator : AbstractValidator<CreateSparePartCommand>
{
    public CreateSparePartCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PartNumber).MaximumLength(100);
        RuleFor(x => x.SerialNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.WarehouseOrgUnitId).NotEmpty();
    }
}

public sealed class CreateSparePartCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<CreateSparePartCommand, Guid>
{
    public async Task<Guid> Handle(CreateSparePartCommand request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var warehouse = await (
            from ou in db.OrgUnits
            join type in db.OrgUnitTypes on ou.OrgUnitTypeId equals type.Id
            where ou.Id == request.WarehouseOrgUnitId
            select new { ou.CompanyId, type.Code }).FirstOrDefaultAsync(cancellationToken);

        if (warehouse is null || warehouse.CompanyId != request.CompanyId || warehouse.Code != OrgUnitTypeCatalog.Codes.Warehouse)
        {
            throw new ConflictException("El almacén indicado no es válido para esta empresa.");
        }

        var serialTaken = await db.SpareParts.AnyAsync(
            p => p.CompanyId == request.CompanyId && p.SerialNumber == request.SerialNumber, cancellationToken);
        if (serialTaken)
        {
            throw new ConflictException("Ya existe una refacción con ese número de serie en esta empresa.");
        }

        var sparePart = SparePart.Create(
            request.CompanyId, request.Name, request.PartNumber, request.SerialNumber, request.WarehouseOrgUnitId,
            clock.UtcNow, currentUser.UserId);

        db.SpareParts.Add(sparePart);
        await db.SaveChangesAsync(cancellationToken);

        return sparePart.Id;
    }
}
