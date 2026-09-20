using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.SparePartsAndConsumables;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.SparePartsAndConsumables;

public sealed record UpdateConsumableCommand(Guid ConsumableId, string Name, string? Sku, string UnitOfMeasure, decimal? MinimumStock)
    : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Consumables.Update;
}

public sealed class UpdateConsumableCommandValidator : AbstractValidator<UpdateConsumableCommand>
{
    public UpdateConsumableCommandValidator()
    {
        RuleFor(x => x.ConsumableId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).MaximumLength(100);
        RuleFor(x => x.UnitOfMeasure).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0).When(x => x.MinimumStock is not null);
    }
}

public sealed class UpdateConsumableCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<UpdateConsumableCommand>
{
    public async Task Handle(UpdateConsumableCommand request, CancellationToken cancellationToken)
    {
        var consumable = await db.Consumables.FirstOrDefaultAsync(c => c.Id == request.ConsumableId, cancellationToken)
            ?? throw new NotFoundException(nameof(Consumable), request.ConsumableId);

        if (!currentCompany.AccessibleCompanyIds.Contains(consumable.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este consumible.");
        }

        consumable.UpdateProfile(request.Name, request.Sku, request.UnitOfMeasure, request.MinimumStock, clock.UtcNow, currentUser.UserId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
