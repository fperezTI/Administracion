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
/// Atomic "return current custodian + assign to a new one" (pedido: reasignar sin dos pasos visibles).
/// Composes exactly the same domain calls as <see cref="ReturnAssignmentCommand"/> followed by
/// <see cref="CreateAssignmentCommand"/> inside a single SaveChanges, chaining
/// Assigned → InWarehouse → Reserved — both legal transitions in <see cref="AssetStateMachine"/> — so the
/// asset never needs a direct Assigned → Reserved edge. The new assignment still starts
/// <see cref="AssignmentStatus.PendingSignature"/>: the recipient must accept it via
/// <see cref="SignAssignmentCommand"/> like any other assignment, the digital signature flow is unchanged.
/// </summary>
public sealed record ReassignAssetCommand(
    Guid AssetId, Guid NewAssignedToUserId, Guid? OrgUnitId, string TypedFullName, string? Notes,
    IReadOnlyList<Guid>? AccessoryAssetIds = null)
    : IRequest<CreateAssignmentResult>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Assignments.Reassign;
}

public sealed class ReassignAssetCommandValidator : AbstractValidator<ReassignAssetCommand>
{
    public ReassignAssetCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.NewAssignedToUserId).NotEmpty();
        RuleFor(x => x.TypedFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class ReassignAssetCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IFolioGenerator folioGenerator, IClock clock, INotificationSender notificationSender, IFrontendLinkBuilder linkBuilder)
    : IRequestHandler<ReassignAssetCommand, CreateAssignmentResult>
{
    public async Task<CreateAssignmentResult> Handle(ReassignAssetCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión para reasignar un activo.");
        }

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        if (!currentCompany.AccessibleCompanyIds.Contains(asset.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este activo.");
        }

        var currentAssignment = await db.Assignments
            .FirstOrDefaultAsync(a => a.AssetId == asset.Id && a.Status == AssignmentStatus.Accepted, cancellationToken)
            ?? throw new ConflictException("El activo no tiene una asignación activa; usa 'Asignar' en su lugar.");

        var recipientHasAccess = await db.UserCompanies
            .AnyAsync(uc => uc.UserId == request.NewAssignedToUserId && uc.CompanyId == asset.CompanyId, cancellationToken);
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

        var oldGroupMembers = await AssignmentGroupSupport.GetGroupMembersAsync(
            db, currentAssignment, AssignmentStatus.Accepted, cancellationToken);
        await AssignmentGroupSupport.ReturnGroupAsync(
            db, folioGenerator, currentUser, oldGroupMembers, request.TypedFullName, request.Notes, now, userId,
            cancellationToken);

        var accessories = await AssignmentGroupSupport.ValidateAccessoriesAsync(
            db, asset, request.AccessoryAssetIds, cancellationToken);

        var (newAssignment, assignMovement) = await AssignmentGroupSupport.CreateGroupAsync(
            db, folioGenerator, notificationSender, linkBuilder, asset, accessories, request.NewAssignedToUserId,
            request.OrgUnitId, request.Notes, now, userId, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return new CreateAssignmentResult(newAssignment.Id, assignMovement.Id, assignMovement.FolioNumber);
    }
}
