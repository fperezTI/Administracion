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
/// Requests the final disposition of an already-decommissioned asset. Unlike decommission, this does
/// <em>not</em> move the asset's status while pending — <see cref="AssetStatus.Decommissioned"/> is
/// already a valid resting state, so there is no separate "pending disposal" status to introduce. The
/// disposal type is encoded in the approval's <c>ContextType</c> (<c>"AssetDisposal:Sold"</c> etc.)
/// instead of a side table, since <see cref="AssetApprovalReactionHandler"/> needs to know the target
/// status once approved and nothing else in the system needs to query "pending disposals" by type yet.
/// </summary>
public sealed record RequestAssetDisposalCommand(Guid AssetId, AssetStatus TargetStatus, string Justification)
    : IRequest<Guid>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Assets.Decommission;
}

public sealed class RequestAssetDisposalCommandValidator : AbstractValidator<RequestAssetDisposalCommand>
{
    public RequestAssetDisposalCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Justification).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.TargetStatus).Must(s => s is AssetStatus.Sold or AssetStatus.Donated or AssetStatus.Destroyed)
            .WithMessage("El destino de disposición debe ser Sold, Donated o Destroyed.");
    }
}

public sealed class RequestAssetDisposalCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IApprovalCoordinator approvalCoordinator)
    : IRequestHandler<RequestAssetDisposalCommand, Guid>
{
    public async Task<Guid> Handle(RequestAssetDisposalCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        if (!currentCompany.AccessibleCompanyIds.Contains(asset.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este activo.");
        }

        if (asset.Status != AssetStatus.Decommissioned)
        {
            throw new ConflictException("Solo un activo dado de baja puede solicitarse para disposición.");
        }

        var approvalInstanceId = await approvalCoordinator.RequestApprovalAsync(
            asset.CompanyId, "asset.disposal", $"AssetDisposal:{request.TargetStatus}", asset.Id,
            currentUser.UserId!.Value, request.Justification, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return approvalInstanceId;
    }
}
