using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets;

/// <summary>
/// Marks one asset as an accessory of another (e.g. a charger of a laptop), restricted to a single level
/// in both directions — the primary can't itself be an accessory, and the accessory can't already have
/// accessories of its own — so a chain deeper than one hop can never form. Assigning the primary later
/// cascades to every asset linked here, see AssetManagement.Application.Inventory.AssignmentGroupSupport.
/// </summary>
public sealed record LinkAssetAccessoryCommand(Guid PrimaryAssetId, Guid AccessoryAssetId)
    : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assets.Update;
}

public sealed class LinkAssetAccessoryCommandValidator : AbstractValidator<LinkAssetAccessoryCommand>
{
    public LinkAssetAccessoryCommandValidator()
    {
        RuleFor(x => x.PrimaryAssetId).NotEmpty();
        RuleFor(x => x.AccessoryAssetId).NotEmpty();
    }
}

public sealed class LinkAssetAccessoryCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<LinkAssetAccessoryCommand>
{
    public async Task Handle(LinkAssetAccessoryCommand request, CancellationToken cancellationToken)
    {
        if (request.PrimaryAssetId == request.AccessoryAssetId)
        {
            throw new ConflictException("Un activo no puede ser accesorio de sí mismo.");
        }

        var primary = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.PrimaryAssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.PrimaryAssetId);

        if (!currentCompany.AccessibleCompanyIds.Contains(primary.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este activo.");
        }

        var accessory = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AccessoryAssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AccessoryAssetId);

        if (accessory.CompanyId != primary.CompanyId)
        {
            throw new ConflictException("El accesorio debe pertenecer a la misma empresa que el activo principal.");
        }

        if (primary.AccessoryOfAssetId is not null)
        {
            throw new ConflictException("Un activo que ya es accesorio de otro no puede tener sus propios accesorios.");
        }

        if (accessory.AccessoryOfAssetId is not null)
        {
            throw new ConflictException("Este activo ya es accesorio de otro — quítalo primero.");
        }

        var accessoryHasItsOwnAccessories = await db.Assets
            .AnyAsync(a => a.AccessoryOfAssetId == accessory.Id, cancellationToken);
        if (accessoryHasItsOwnAccessories)
        {
            throw new ConflictException("Un activo con accesorios propios no puede ser vinculado como accesorio de otro.");
        }

        var now = clock.UtcNow;
        accessory.LinkAsAccessoryOf(primary.Id, now, currentUser.UserId);

        await db.SaveChangesAsync(cancellationToken);
    }
}
