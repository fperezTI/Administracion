using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets.Categories;

public sealed record SetAssetCategoryActiveCommand(Guid AssetCategoryId, bool IsActive) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Catalogs.Update;
}

public sealed class SetAssetCategoryActiveCommandHandler(IApplicationDbContext db) : IRequestHandler<SetAssetCategoryActiveCommand>
{
    public async Task Handle(SetAssetCategoryActiveCommand request, CancellationToken cancellationToken)
    {
        var category = await db.AssetCategories.FirstOrDefaultAsync(c => c.Id == request.AssetCategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(AssetCategory), request.AssetCategoryId);

        if (request.IsActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
