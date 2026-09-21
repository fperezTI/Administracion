using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>
/// Self-service acceptance of a pending assignment: the recipient signs their own receipt. Deliberately
/// does not implement <see cref="IRequiresPermission"/> — anyone can sign, but only for their own
/// assignment (see AuthorizationBehavior's comment re: GetMeQuery-style self-scoped requests). This is
/// what finally moves the asset to <see cref="AssetStatus.Assigned"/> and completes the pending Movement.
/// </summary>
public sealed record SignAssignmentCommand(Guid AssignmentId, string TypedFullName) : IRequest
{
}

public sealed class SignAssignmentCommandValidator : AbstractValidator<SignAssignmentCommand>
{
    public SignAssignmentCommandValidator()
    {
        RuleFor(x => x.AssignmentId).NotEmpty();
        RuleFor(x => x.TypedFullName).NotEmpty().MaximumLength(200);
    }
}

public sealed class SignAssignmentCommandHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<SignAssignmentCommand>
{
    public async Task Handle(SignAssignmentCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión para firmar una asignación.");
        }

        var assignment = await db.Assignments.FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Assignment), request.AssignmentId);

        if (assignment.AssignedToUserId != userId)
        {
            throw new ForbiddenAccessException("Solo el destinatario de la asignación puede firmarla.");
        }

        var now = clock.UtcNow;

        var groupMembers = await AssignmentGroupSupport.GetGroupMembersAsync(
            db, assignment, AssignmentStatus.PendingSignature, cancellationToken);

        foreach (var member in groupMembers)
        {
            var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == member.AssetId, cancellationToken)
                ?? throw new NotFoundException(nameof(Asset), member.AssetId);

            var movement = await db.Movements.FirstOrDefaultAsync(m => m.Id == member.MovementId, cancellationToken)
                ?? throw new NotFoundException(nameof(Movement), member.MovementId);

            var payload = $"AssignmentAcceptance|{member.Id}|{asset.Id}|{userId}|{now:O}";
            var signature = Domain.Signature.SignatureRecord.Create(
                asset.CompanyId, "AssignmentAcceptance", member.Id, userId, request.TypedFullName.Trim(),
                currentUser.IpAddress, currentUser.UserAgent, SignatureHasher.Hash(payload),
                Domain.Signature.SignatureRecord.TypedConfirmationMechanism, null, now, userId);

            member.Accept(signature.Id, now, userId);
            movement.Complete(now, userId);
            asset.ChangeStatus(AssetStatus.Assigned, now, userId);

            db.SignatureRecords.Add(signature);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
