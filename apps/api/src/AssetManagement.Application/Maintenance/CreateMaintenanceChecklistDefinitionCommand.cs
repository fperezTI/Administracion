using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Maintenance;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record CreateMaintenanceChecklistDefinitionCommand(
    string Key, string Name, Guid? AssetCategoryId, IReadOnlyList<string> InitialItems)
    : IRequest<Guid>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Maintenance.Create;
}

public sealed class CreateMaintenanceChecklistDefinitionCommandValidator
    : AbstractValidator<CreateMaintenanceChecklistDefinitionCommand>
{
    public CreateMaintenanceChecklistDefinitionCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InitialItems).NotEmpty();
        RuleForEach(x => x.InitialItems).NotEmpty().MaximumLength(500);
    }
}

public sealed class CreateMaintenanceChecklistDefinitionCommandHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<CreateMaintenanceChecklistDefinitionCommand, Guid>
{
    public async Task<Guid> Handle(CreateMaintenanceChecklistDefinitionCommand request, CancellationToken cancellationToken)
    {
        var keyTaken = await db.MaintenanceChecklistDefinitions.AnyAsync(c => c.Key == request.Key, cancellationToken);
        if (keyTaken)
        {
            throw new ConflictException("Ya existe un checklist con esa clave.");
        }

        if (request.AssetCategoryId is { } categoryId)
        {
            var categoryExists = await db.AssetCategories.AnyAsync(c => c.Id == categoryId, cancellationToken);
            if (!categoryExists)
            {
                throw new NotFoundException(nameof(Domain.Assets.AssetCategory), categoryId);
            }
        }

        var now = clock.UtcNow;
        var checklist = MaintenanceChecklistDefinition.Create(
            request.Key, request.Name, request.AssetCategoryId, request.InitialItems, now, currentUser.UserId);

        db.MaintenanceChecklistDefinitions.Add(checklist);
        await db.SaveChangesAsync(cancellationToken);

        return checklist.Id;
    }
}
