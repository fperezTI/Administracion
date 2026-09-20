using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets.Categories;

public sealed record AddCustomFieldDefinitionCommand(
    Guid AssetCategoryId, string Name, string Code, CustomFieldDataType DataType, bool IsRequired, string? Options)
    : IRequest<Guid>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Catalogs.Update;
}

public sealed class AddCustomFieldDefinitionCommandValidator : AbstractValidator<AddCustomFieldDefinitionCommand>
{
    public AddCustomFieldDefinitionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
    }
}

public sealed class AddCustomFieldDefinitionCommandHandler(IApplicationDbContext db)
    : IRequestHandler<AddCustomFieldDefinitionCommand, Guid>
{
    public async Task<Guid> Handle(AddCustomFieldDefinitionCommand request, CancellationToken cancellationToken)
    {
        var category = await db.AssetCategories
            .Include(c => c.CustomFields)
            .FirstOrDefaultAsync(c => c.Id == request.AssetCategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(AssetCategory), request.AssetCategoryId);

        var field = category.AddCustomField(request.Name, request.Code, request.DataType, request.IsRequired, request.Options);
        await db.SaveChangesAsync(cancellationToken);

        return field.Id;
    }
}
