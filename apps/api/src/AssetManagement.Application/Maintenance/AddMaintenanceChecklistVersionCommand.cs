using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Maintenance;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record AddMaintenanceChecklistVersionCommand(Guid ChecklistDefinitionId, IReadOnlyList<string> Items)
    : IRequest<int>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Maintenance.Update;
}

public sealed class AddMaintenanceChecklistVersionCommandValidator : AbstractValidator<AddMaintenanceChecklistVersionCommand>
{
    public AddMaintenanceChecklistVersionCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).NotEmpty().MaximumLength(500);
    }
}

public sealed class AddMaintenanceChecklistVersionCommandHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<AddMaintenanceChecklistVersionCommand, int>
{
    public async Task<int> Handle(AddMaintenanceChecklistVersionCommand request, CancellationToken cancellationToken)
    {
        var checklist = await db.MaintenanceChecklistDefinitions.Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.Id == request.ChecklistDefinitionId, cancellationToken)
            ?? throw new NotFoundException(nameof(MaintenanceChecklistDefinition), request.ChecklistDefinitionId);

        var version = checklist.AddVersion(request.Items, clock.UtcNow, currentUser.UserId);
        await db.SaveChangesAsync(cancellationToken);

        return version.VersionNumber;
    }
}
