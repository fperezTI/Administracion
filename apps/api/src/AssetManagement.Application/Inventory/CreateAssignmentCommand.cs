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
/// Starts custody of an asset (pedido §13). Deliberately does not put the asset in
/// <see cref="AssetStatus.Assigned"/> yet — only <see cref="AssetStatus.Reserved"/> — because
/// docs/architecture/domain-model.md requires an accepted signature before an assignment is final; see
/// <see cref="SignAssignmentCommand"/>.
/// </summary>
public sealed record CreateAssignmentCommand(Guid AssetId, Guid AssignedToUserId, Guid? OrgUnitId, string? Notes)
    : IRequest<CreateAssignmentResult>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Assignments.Create;
}

public sealed record CreateAssignmentResult(Guid AssignmentId, Guid MovementId, string MovementFolio);

public sealed class CreateAssignmentCommandValidator : AbstractValidator<CreateAssignmentCommand>
{
    public CreateAssignmentCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.AssignedToUserId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class CreateAssignmentCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IFolioGenerator folioGenerator, IClock clock)
    : IRequestHandler<CreateAssignmentCommand, CreateAssignmentResult>
{
    public async Task<CreateAssignmentResult> Handle(CreateAssignmentCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        if (!currentCompany.AccessibleCompanyIds.Contains(asset.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este activo.");
        }

        var recipientHasAccess = await db.UserCompanies
            .AnyAsync(uc => uc.UserId == request.AssignedToUserId && uc.CompanyId == asset.CompanyId, cancellationToken);
        if (!recipientHasAccess)
        {
            throw new ConflictException("El destinatario no tiene acceso a la empresa de este activo.");
        }

        if (request.OrgUnitId is { } orgUnitId)
        {
            var orgUnitExists = await db.OrgUnits
                .AnyAsync(o => o.Id == orgUnitId && o.CompanyId == asset.CompanyId, cancellationToken);
            if (!orgUnitExists)
            {
                throw new NotFoundException(nameof(OrgUnit), orgUnitId);
            }
        }

        var now = clock.UtcNow;
        var folio = await folioGenerator.NextAsync(asset.CompanyId, FolioDocumentTypes.MovementAssignment, cancellationToken);

        var movement = Movement.Create(
            asset.CompanyId, asset.Id, MovementType.Assignment, folio, startsCompleted: false,
            fromOrgUnitId: asset.CurrentOrgUnitId, toOrgUnitId: request.OrgUnitId, fromUserId: null,
            toUserId: request.AssignedToUserId, notes: request.Notes, now, currentUser.UserId);

        var assignment = Domain.Inventory.Assignment.Create(
            asset.CompanyId, asset.Id, request.AssignedToUserId, request.OrgUnitId, movement.Id, now, currentUser.UserId);

        asset.ChangeStatus(AssetStatus.Reserved, now, currentUser.UserId);

        db.Movements.Add(movement);
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateAssignmentResult(assignment.Id, movement.Id, movement.FolioNumber);
    }
}
