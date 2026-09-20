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
/// Completes a cross-company transfer ("recepción con firma", multi-company.md). Requires
/// Transfers.Update AND real access to the destination company — this is the one place a transfer is
/// actually protected against being "received" by the wrong company, since anyone can see any company in
/// the tenant-wide company list. Regenerates the asset's InternalFolio in the destination company's own
/// sequence (see ADR 0007) — AssetTag.Code is untouched.
/// </summary>
public sealed record ReceiveCrossCompanyTransferCommand(
    Guid TransferId, string SignatureMechanism, string? TypedFullName, string? SignatureImageDataUrl)
    : IRequest, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Transfers.Update;
}

public sealed class ReceiveCrossCompanyTransferCommandValidator : AbstractValidator<ReceiveCrossCompanyTransferCommand>
{
    public ReceiveCrossCompanyTransferCommandValidator()
    {
        RuleFor(x => x.TransferId).NotEmpty();
        RuleFor(x => x.SignatureMechanism).NotEmpty();
    }
}

public sealed class ReceiveCrossCompanyTransferCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IFolioGenerator folioGenerator, IClock clock)
    : IRequestHandler<ReceiveCrossCompanyTransferCommand>
{
    public async Task Handle(ReceiveCrossCompanyTransferCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión para recibir una transferencia.");
        }

        var transfer = await db.Transfers.FirstOrDefaultAsync(t => t.Id == request.TransferId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transfer), request.TransferId);

        if (!currentCompany.AccessibleCompanyIds.Contains(transfer.ToCompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa destino de esta transferencia.");
        }

        // IgnoreQueryFilters is deliberate: at this point the asset's CompanyId is still the SOURCE
        // company, which the receiving user may have no access to — that's expected, they're receiving
        // *into* ToCompanyId, not reading from the source. Authorization already happened above via the
        // ToCompanyId membership check, not via this query filter.
        var asset = await db.Assets.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == transfer.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), transfer.AssetId);

        var now = clock.UtcNow;
        var newInternalFolio = await folioGenerator.NextAsync(transfer.ToCompanyId, FolioDocumentTypes.Asset, cancellationToken);
        var movementFolio = await folioGenerator.NextAsync(transfer.ToCompanyId, FolioDocumentTypes.MovementCrossCompanyTransferIn, cancellationToken);

        var signature = SignatureFactory.Create(
            transfer.ToCompanyId, "CrossCompanyTransferReceipt", transfer.Id, userId, currentUser.DisplayName ?? "Usuario",
            request.SignatureMechanism, request.TypedFullName, request.SignatureImageDataUrl, currentUser.IpAddress,
            currentUser.UserAgent, now);

        var receiptMovement = Movement.Create(
            transfer.ToCompanyId, asset.Id, MovementType.CrossCompanyTransferIn, movementFolio, startsCompleted: true,
            fromOrgUnitId: null, toOrgUnitId: null, fromUserId: null, toUserId: null, notes: null, now, userId,
            fromCompanyId: transfer.FromCompanyId);

        asset.CompleteCrossCompanyTransfer(transfer.ToCompanyId, newInternalFolio, now, userId);
        transfer.Receive(receiptMovement.Id, signature.Id, now, userId);

        db.Movements.Add(receiptMovement);
        db.SignatureRecords.Add(signature);
        await db.SaveChangesAsync(cancellationToken);
    }
}
