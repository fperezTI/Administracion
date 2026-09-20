using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Requests;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Requests;

public sealed record GetInternalRequestByIdQuery(Guid InternalRequestId) : IRequest<InternalRequestDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Requests.Read;
}

public sealed record InternalRequestDetail(
    Guid Id, InternalRequestType Type, Guid AssetId, string AssetFolio, Guid RequestedByUserId,
    string RequestedByDisplayName, string Justification, DateOnly? ExpectedReturnDate, InternalRequestStatus Status,
    Guid? FulfillmentReferenceId, DateTimeOffset RequestedAtUtc, DateTimeOffset? DecidedAtUtc);

public sealed class GetInternalRequestByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetInternalRequestByIdQuery, InternalRequestDetail>
{
    public async Task<InternalRequestDetail> Handle(GetInternalRequestByIdQuery request, CancellationToken cancellationToken)
    {
        var internalRequest = await db.InternalRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.InternalRequestId, cancellationToken)
            ?? throw new NotFoundException(nameof(InternalRequest), request.InternalRequestId);

        var asset = await db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == internalRequest.AssetId, cancellationToken);
        var requester = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == internalRequest.RequestedByUserId, cancellationToken);

        return new InternalRequestDetail(
            internalRequest.Id, internalRequest.Type, internalRequest.AssetId, asset?.InternalFolio ?? "(activo eliminado)",
            internalRequest.RequestedByUserId, requester?.DisplayName ?? "(usuario eliminado)", internalRequest.Justification,
            internalRequest.ExpectedReturnDate, internalRequest.Status, internalRequest.FulfillmentReferenceId,
            internalRequest.RequestedAtUtc, internalRequest.DecidedAtUtc);
    }
}
