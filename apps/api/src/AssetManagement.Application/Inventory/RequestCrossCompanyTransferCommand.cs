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
/// Starts a cross-company transfer (pedido/multi-company.md: "aprobación + salida + tránsito + recepción
/// con firma"). Only the requester's access to the source company is checked here — the destination
/// company is picked from the tenant-wide company list (Company has no membership query filter, same as
/// today's /companies screen), and is protected on the other end instead: only someone with
/// Transfers.Update AND real access to the destination company can receive it
/// (<see cref="ReceiveCrossCompanyTransferCommand"/>). See the F5 plan's scope decisions for why only
/// InWarehouse assets qualify.
/// </summary>
public sealed record RequestCrossCompanyTransferCommand(Guid AssetId, Guid ToCompanyId, string? Notes)
    : IRequest<Guid>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Transfers.Create;
}

public sealed class RequestCrossCompanyTransferCommandValidator : AbstractValidator<RequestCrossCompanyTransferCommand>
{
    public RequestCrossCompanyTransferCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.ToCompanyId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class RequestCrossCompanyTransferCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IApprovalCoordinator approvalCoordinator, IClock clock)
    : IRequestHandler<RequestCrossCompanyTransferCommand, Guid>
{
    public async Task<Guid> Handle(RequestCrossCompanyTransferCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        if (!currentCompany.AccessibleCompanyIds.Contains(asset.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este activo.");
        }

        if (asset.Status != AssetStatus.InWarehouse)
        {
            throw new ConflictException("Solo un activo en almacén puede transferirse entre empresas.");
        }

        if (request.ToCompanyId == asset.CompanyId)
        {
            throw new ConflictException("La empresa destino debe ser distinta de la empresa de origen.");
        }

        var destinationExists = await db.Companies.AnyAsync(c => c.Id == request.ToCompanyId, cancellationToken);
        if (!destinationExists)
        {
            throw new NotFoundException(nameof(Domain.Organization.Company), request.ToCompanyId);
        }

        var now = clock.UtcNow;
        var transfer = Domain.Inventory.Transfer.Create(
            asset.Id, asset.CompanyId, request.ToCompanyId, currentUser.UserId!.Value, request.Notes, now, currentUser.UserId);
        db.Transfers.Add(transfer);

        await approvalCoordinator.RequestApprovalAsync(
            asset.CompanyId, "asset.cross-company-transfer", "CrossCompanyTransfer", transfer.Id,
            currentUser.UserId!.Value, request.Notes, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return transfer.Id;
    }
}
