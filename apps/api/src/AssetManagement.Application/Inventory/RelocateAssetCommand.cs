using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using AssetManagement.Domain.Organization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>
/// Moves an asset between org units within the same company ("transferencia intra-empresa", pedido
/// §13/roadmap F3 — not to be confused with the cross-company <c>Transfer</c> aggregate reserved for
/// F5). Replaces the free-form <c>CurrentOrgUnitId</c> edit that used to live in
/// UpdateAssetGeneralInfoCommand: same permission as before (<see cref="PermissionCatalog.Assets.Update"/>,
/// so nobody's access changes), but now every relocation leaves an audited <see cref="Movement"/> instead
/// of a silent field edit. Does not change <see cref="Asset.Status"/> — only where the asset physically is.
/// </summary>
public sealed record RelocateAssetCommand(Guid AssetId, Guid? NewOrgUnitId, string? Notes)
    : IRequest<RelocateAssetResult>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Assets.Update;
}

public sealed record RelocateAssetResult(Guid MovementId, string MovementFolio);

public sealed class RelocateAssetCommandValidator : AbstractValidator<RelocateAssetCommand>
{
    public RelocateAssetCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class RelocateAssetCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IFolioGenerator folioGenerator, IClock clock)
    : IRequestHandler<RelocateAssetCommand, RelocateAssetResult>
{
    public async Task<RelocateAssetResult> Handle(RelocateAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        if (!currentCompany.AccessibleCompanyIds.Contains(asset.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este activo.");
        }

        if (request.NewOrgUnitId is { } orgUnitId)
        {
            var orgUnitExists = await db.OrgUnits
                .AnyAsync(o => o.Id == orgUnitId && o.CompanyId == asset.CompanyId, cancellationToken);
            if (!orgUnitExists)
            {
                throw new NotFoundException(nameof(OrgUnit), orgUnitId);
            }
        }

        if (request.NewOrgUnitId == asset.CurrentOrgUnitId)
        {
            throw new ConflictException("El activo ya está en esa unidad organizacional.");
        }

        var now = clock.UtcNow;
        var folio = await folioGenerator.NextAsync(asset.CompanyId, FolioDocumentTypes.MovementRelocation, cancellationToken);

        var movement = Movement.Create(
            asset.CompanyId, asset.Id, MovementType.Relocation, folio, startsCompleted: true,
            fromOrgUnitId: asset.CurrentOrgUnitId, toOrgUnitId: request.NewOrgUnitId, fromUserId: null,
            toUserId: null, notes: request.Notes, now, currentUser.UserId);

        asset.MoveToOrgUnit(request.NewOrgUnitId, now, currentUser.UserId);

        db.Movements.Add(movement);
        await db.SaveChangesAsync(cancellationToken);

        return new RelocateAssetResult(movement.Id, movement.FolioNumber);
    }
}
