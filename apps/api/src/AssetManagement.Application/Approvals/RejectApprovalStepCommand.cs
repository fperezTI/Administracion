using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Approvals;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Approvals;

public sealed record RejectApprovalStepCommand(
    Guid ApprovalInstanceId, string Comment, string SignatureMechanism, string? TypedFullName, string? SignatureImageDataUrl)
    : IRequest, IAuditableCommand
{
}

public sealed class RejectApprovalStepCommandValidator : AbstractValidator<RejectApprovalStepCommand>
{
    public RejectApprovalStepCommandValidator()
    {
        RuleFor(x => x.ApprovalInstanceId).NotEmpty();
        RuleFor(x => x.SignatureMechanism).NotEmpty();
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(1000);
    }
}

public sealed class RejectApprovalStepCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<RejectApprovalStepCommand>
{
    public async Task Handle(RejectApprovalStepCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión para decidir sobre una aprobación.");
        }

        var instance = await db.ApprovalInstances.FirstOrDefaultAsync(i => i.Id == request.ApprovalInstanceId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApprovalInstance), request.ApprovalInstanceId);

        if (!currentCompany.AccessibleCompanyIds.Contains(instance.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de esta aprobación.");
        }

        var myRoleIds = await db.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToListAsync(cancellationToken);

        var now = clock.UtcNow;
        var signature = SignatureFactory.Create(
            instance.CompanyId, "ApprovalDecision", instance.Id, userId, currentUser.DisplayName ?? "Usuario",
            request.SignatureMechanism, request.TypedFullName, request.SignatureImageDataUrl, currentUser.IpAddress,
            currentUser.UserAgent, now);

        instance.Decide(userId, myRoleIds, ApprovalStepDecision.Rejected, request.Comment, now, userId);

        db.SignatureRecords.Add(signature);
        await db.SaveChangesAsync(cancellationToken);
    }
}
