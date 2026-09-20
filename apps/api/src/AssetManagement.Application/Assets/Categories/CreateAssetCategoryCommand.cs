using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets.Categories;

public sealed record CreateAssetCategoryCommand(string Name, string Code, IdentificationTechnology DefaultIdentificationTechnology)
    : IRequest<Guid>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Catalogs.Create;
}

public sealed class CreateAssetCategoryCommandValidator : AbstractValidator<CreateAssetCategoryCommand>
{
    public CreateAssetCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateAssetCategoryCommandHandler(IApplicationDbContext db, IClock clock)
    : IRequestHandler<CreateAssetCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateAssetCategoryCommand request, CancellationToken cancellationToken)
    {
        var codeTaken = await db.AssetCategories.AnyAsync(c => c.Code == request.Code, cancellationToken);
        if (codeTaken)
        {
            throw new ConflictException($"Ya existe una categoría con el código '{request.Code}'.");
        }

        var category = AssetCategory.Create(request.Name, request.Code, request.DefaultIdentificationTechnology, clock.UtcNow);
        db.AssetCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
