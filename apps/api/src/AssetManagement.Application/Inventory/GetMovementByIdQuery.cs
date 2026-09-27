using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

public sealed record GetMovementByIdQuery(Guid MovementId) : IRequest<MovementDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Movements.Read;
}

public sealed record MovementDetail(
    Guid Id,
    Guid CompanyId,
    Guid AssetId,
    string AssetFolio,
    MovementType Type,
    string FolioNumber,
    MovementStatus Status,
    Guid? FromOrgUnitId,
    string? FromOrgUnitName,
    Guid? ToOrgUnitId,
    string? ToOrgUnitName,
    Guid? FromUserId,
    string? FromUserDisplayName,
    Guid? ToUserId,
    string? ToUserDisplayName,
    string? Notes,
    DateTimeOffset EffectiveAtUtc);

public sealed class GetMovementByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetMovementByIdQuery, MovementDetail>
{
    public async Task<MovementDetail> Handle(GetMovementByIdQuery request, CancellationToken cancellationToken)
    {
        // Movement's own query filter (AccessibleCompanyIds) already scopes this to companies the
        // caller belongs to — nothing extra to validate here (mirrors GetMovementsQuery).
        var movement = await db.Movements.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.MovementId, cancellationToken)
            ?? throw new NotFoundException(nameof(Movement), request.MovementId);

        var asset = await db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == movement.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Assets.Asset), movement.AssetId);

        string? fromOrgUnitName = movement.FromOrgUnitId is { } fromOrgUnitId
            ? await db.OrgUnits.AsNoTracking().Where(o => o.Id == fromOrgUnitId).Select(o => o.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        string? toOrgUnitName = movement.ToOrgUnitId is { } toOrgUnitId
            ? await db.OrgUnits.AsNoTracking().Where(o => o.Id == toOrgUnitId).Select(o => o.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        string? fromUserDisplayName = movement.FromUserId is { } fromUserId
            ? await db.Users.AsNoTracking().Where(u => u.Id == fromUserId).Select(u => u.DisplayName).FirstOrDefaultAsync(cancellationToken)
            : null;

        string? toUserDisplayName = movement.ToUserId is { } toUserId
            ? await db.Users.AsNoTracking().Where(u => u.Id == toUserId).Select(u => u.DisplayName).FirstOrDefaultAsync(cancellationToken)
            : null;

        return new MovementDetail(
            movement.Id, movement.CompanyId, movement.AssetId, asset.InternalFolio, movement.Type, movement.FolioNumber, movement.Status,
            movement.FromOrgUnitId, fromOrgUnitName, movement.ToOrgUnitId, toOrgUnitName,
            movement.FromUserId, fromUserDisplayName, movement.ToUserId, toUserDisplayName,
            movement.Notes, movement.EffectiveAtUtc);
    }
}
