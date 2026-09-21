using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Inventory;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>
/// Records that an assigned asset came back (pedido §13). Requires a signature too
/// (docs/architecture/domain-model.md: "Requiere firma de devolución") — but here it is the signature of
/// whoever executes the return (typically IT/warehouse receiving the item), not a second self-service
/// step for the outgoing custodian — a deliberate V1 simplification, see the F3 plan.
/// </summary>
public sealed record ReturnAssignmentCommand(Guid AssignmentId, string TypedFullName, string? Notes)
    : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Returns.Create;
}

public sealed class ReturnAssignmentCommandValidator : AbstractValidator<ReturnAssignmentCommand>
{
    public ReturnAssignmentCommandValidator()
    {
        RuleFor(x => x.AssignmentId).NotEmpty();
        RuleFor(x => x.TypedFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class ReturnAssignmentCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IFolioGenerator folioGenerator, IClock clock)
    : IRequestHandler<ReturnAssignmentCommand>
{
    public async Task Handle(ReturnAssignmentCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión para registrar una devolución.");
        }

        var assignment = await db.Assignments.FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Assignment), request.AssignmentId);

        if (!currentCompany.AccessibleCompanyIds.Contains(assignment.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de esta asignación.");
        }

        var now = clock.UtcNow;

        var groupMembers = await AssignmentGroupSupport.GetGroupMembersAsync(
            db, assignment, AssignmentStatus.Accepted, cancellationToken);
        await AssignmentGroupSupport.ReturnGroupAsync(
            db, folioGenerator, currentUser, groupMembers, request.TypedFullName, request.Notes, now, userId,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }
}
