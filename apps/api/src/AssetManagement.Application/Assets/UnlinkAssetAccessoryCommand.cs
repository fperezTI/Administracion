using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets;

public sealed record UnlinkAssetAccessoryCommand(Guid AccessoryAssetId) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assets.Update;
}

public sealed class UnlinkAssetAccessoryCommandValidator : AbstractValidator<UnlinkAssetAccessoryCommand>
{
    public UnlinkAssetAccessoryCommandValidator()
    {
        RuleFor(x => x.AccessoryAssetId).NotEmpty();
    }
}

public sealed class UnlinkAssetAccessoryCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<UnlinkAssetAccessoryCommand>
{
    public async Task Handle(UnlinkAssetAccessoryCommand request, CancellationToken cancellationToken)
    {
        var accessory = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AccessoryAssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AccessoryAssetId);

        if (!currentCompany.AccessibleCompanyIds.Contains(accessory.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este activo.");
        }

        var now = clock.UtcNow;
        accessory.UnlinkAccessory(now, currentUser.UserId);

        await db.SaveChangesAsync(cancellationToken);
    }
}
