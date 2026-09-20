using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

public sealed record GetTransferByIdQuery(Guid TransferId) : IRequest<TransferDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Transfers.Read;
}

public sealed record TransferDetail(
    Guid Id,
    Guid AssetId,
    string AssetFolio,
    Guid FromCompanyId,
    string FromCompanyName,
    Guid ToCompanyId,
    string ToCompanyName,
    Guid RequestedByUserId,
    string RequestedByDisplayName,
    string? Notes,
    TransferStatus Status,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? DepartedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public sealed class GetTransferByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetTransferByIdQuery, TransferDetail>
{
    public async Task<TransferDetail> Handle(GetTransferByIdQuery request, CancellationToken cancellationToken)
    {
        var transfer = await db.Transfers.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TransferId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transfer), request.TransferId);

        var asset = await db.Assets.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == transfer.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Assets.Asset), transfer.AssetId);

        var fromCompany = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == transfer.FromCompanyId, cancellationToken);
        var toCompany = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == transfer.ToCompanyId, cancellationToken);
        var requester = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == transfer.RequestedByUserId, cancellationToken);

        return new TransferDetail(
            transfer.Id, transfer.AssetId, asset.InternalFolio, transfer.FromCompanyId,
            fromCompany?.TradeName ?? "(empresa eliminada)", transfer.ToCompanyId, toCompany?.TradeName ?? "(empresa eliminada)",
            transfer.RequestedByUserId, requester?.DisplayName ?? "(usuario eliminado)", transfer.Notes, transfer.Status,
            transfer.RequestedAtUtc, transfer.DepartedAtUtc, transfer.CompletedAtUtc);
    }
}
