using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets;

/// <summary>Reprinting never changes the asset's identity (pedido §12) — it only records a new print
/// event on the existing tag.</summary>
public sealed record ReprintAssetTagCommand(Guid AssetId) : IRequest<ReprintAssetTagResult>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assets.Update;
}

public sealed record ReprintAssetTagResult(string Code, int PrintCount);

public sealed class ReprintAssetTagCommandHandler(IApplicationDbContext db, IClock clock)
    : IRequestHandler<ReprintAssetTagCommand, ReprintAssetTagResult>
{
    public async Task<ReprintAssetTagResult> Handle(ReprintAssetTagCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets
            .Include(a => a.Tag)
            .FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        asset.ReprintTag(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return new ReprintAssetTagResult(asset.Tag!.Code, asset.Tag.PrintCount);
    }
}
