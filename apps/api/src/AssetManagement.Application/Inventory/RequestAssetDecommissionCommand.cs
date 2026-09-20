using AssetManagement.Application.Approvals;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>
/// Starts a decommission request (pedido §13/§29: "requiere aprobación + evidencia"). Moves the asset to
/// <see cref="AssetStatus.PendingDecommission"/> immediately — that status already means "pending" — and
/// asks the Approvals engine for a decision via the generic <see cref="IApprovalCoordinator"/> port; this
/// handler has no idea how approval works, only that it eventually gets an
/// <c>ApprovalCompleted</c>/<c>ApprovalRejected</c> event back (see <see cref="AssetApprovalReactionHandler"/>).
/// "Evidencia" here is a required justification text — attaching an actual file depends on Blob Storage
/// (Documents context, F8, not built yet), a deliberate simplification, not an omission.
/// </summary>
public sealed record RequestAssetDecommissionCommand(Guid AssetId, string Justification)
    : IRequest<Guid>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Assets.Decommission;
}

public sealed class RequestAssetDecommissionCommandValidator : AbstractValidator<RequestAssetDecommissionCommand>
{
    public RequestAssetDecommissionCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Justification).NotEmpty().MaximumLength(1000);
    }
}

public sealed class RequestAssetDecommissionCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IApprovalCoordinator approvalCoordinator, IClock clock)
    : IRequestHandler<RequestAssetDecommissionCommand, Guid>
{
    public async Task<Guid> Handle(RequestAssetDecommissionCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        if (!currentCompany.AccessibleCompanyIds.Contains(asset.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este activo.");
        }

        var now = clock.UtcNow;
        asset.ChangeStatus(AssetStatus.PendingDecommission, now, currentUser.UserId);

        var approvalInstanceId = await approvalCoordinator.RequestApprovalAsync(
            asset.CompanyId, "asset.decommission", "AssetDecommission", asset.Id, currentUser.UserId!.Value,
            request.Justification, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return approvalInstanceId;
    }
}
