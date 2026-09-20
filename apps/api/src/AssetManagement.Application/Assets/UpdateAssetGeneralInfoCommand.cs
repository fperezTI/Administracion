using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets;

/// <summary>Covers the general profile, physical condition and custom field values in one operation —
/// these are edited together on a single "edit asset" screen in practice. Current location is
/// deliberately NOT editable here as of F3 — see <see cref="AssetManagement.Application.Inventory.RelocateAssetCommand"/>,
/// which replaced this silent field edit with an audited Movement.</summary>
public sealed record UpdateAssetGeneralInfoCommand(
    Guid AssetId,
    string Brand,
    string Model,
    string? SerialNumber,
    string? Description,
    string? PatrimonialFolio,
    PhysicalCondition PhysicalCondition,
    IReadOnlyDictionary<Guid, string>? CustomFieldValues)
    : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assets.Update;
}

public sealed class UpdateAssetGeneralInfoCommandValidator : AbstractValidator<UpdateAssetGeneralInfoCommand>
{
    public UpdateAssetGeneralInfoCommandValidator()
    {
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.PatrimonialFolio).MaximumLength(100);
    }
}

public sealed class UpdateAssetGeneralInfoCommandHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<UpdateAssetGeneralInfoCommand>
{
    public async Task Handle(UpdateAssetGeneralInfoCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets
            .Include(a => a.CustomFieldValues)
            .FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        var now = clock.UtcNow;

        asset.UpdateProfile(
            request.Brand, request.Model, request.SerialNumber, request.Description, request.PatrimonialFolio,
            now, currentUser.UserId);
        asset.SetCondition(request.PhysicalCondition, now, currentUser.UserId);

        if (request.CustomFieldValues is not null)
        {
            var validFieldIds = await db.CustomFieldDefinitions
                .Where(f => f.AssetCategoryId == asset.AssetCategoryId)
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);

            var invalidKeys = request.CustomFieldValues.Keys.Where(k => !validFieldIds.Contains(k)).ToList();
            if (invalidKeys.Count != 0)
            {
                throw new ConflictException("Uno o más campos personalizados no pertenecen a esta categoría.");
            }

            asset.SetCustomFieldValues(request.CustomFieldValues.Select(kv => (kv.Key, kv.Value)));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
