using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using AssetManagement.Domain.SparePartsAndConsumables;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.SparePartsAndConsumables;

public sealed record InstallSparePartCommand(Guid SparePartId, Guid AssetId, Guid? MaintenanceOrderId)
    : IRequest, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.SpareParts.Update;
}

public sealed class InstallSparePartCommandValidator : AbstractValidator<InstallSparePartCommand>
{
    public InstallSparePartCommandValidator()
    {
        RuleFor(x => x.SparePartId).NotEmpty();
        RuleFor(x => x.AssetId).NotEmpty();
    }
}

public sealed class InstallSparePartCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<InstallSparePartCommand>
{
    public async Task Handle(InstallSparePartCommand request, CancellationToken cancellationToken)
    {
        var sparePart = await db.SpareParts.Include(p => p.Installations)
            .FirstOrDefaultAsync(p => p.Id == request.SparePartId, cancellationToken)
            ?? throw new NotFoundException(nameof(SparePart), request.SparePartId);

        if (!currentCompany.AccessibleCompanyIds.Contains(sparePart.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de esta refacción.");
        }

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        if (asset.CompanyId != sparePart.CompanyId)
        {
            throw new ConflictException("La refacción y el activo deben pertenecer a la misma empresa.");
        }

        if (request.MaintenanceOrderId is { } orderId)
        {
            var orderExists = await db.MaintenanceOrders.AnyAsync(o => o.Id == orderId, cancellationToken);
            if (!orderExists)
            {
                throw new NotFoundException(nameof(MaintenanceOrder), orderId);
            }
        }

        sparePart.Install(request.AssetId, request.MaintenanceOrderId, clock.UtcNow, currentUser.UserId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
