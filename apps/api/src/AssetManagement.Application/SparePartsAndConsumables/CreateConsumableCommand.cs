using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.SparePartsAndConsumables;
using FluentValidation;
using MediatR;

namespace AssetManagement.Application.SparePartsAndConsumables;

public sealed record CreateConsumableCommand(Guid CompanyId, string Name, string? Sku, string UnitOfMeasure, decimal? MinimumStock)
    : IRequest<Guid>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Consumables.Create;
}

public sealed class CreateConsumableCommandValidator : AbstractValidator<CreateConsumableCommand>
{
    public CreateConsumableCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).MaximumLength(100);
        RuleFor(x => x.UnitOfMeasure).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0).When(x => x.MinimumStock is not null);
    }
}

public sealed class CreateConsumableCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<CreateConsumableCommand, Guid>
{
    public async Task<Guid> Handle(CreateConsumableCommand request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var consumable = Consumable.Create(
            request.CompanyId, request.Name, request.Sku, request.UnitOfMeasure, request.MinimumStock, clock.UtcNow,
            currentUser.UserId);

        db.Consumables.Add(consumable);
        await db.SaveChangesAsync(cancellationToken);

        return consumable.Id;
    }
}
