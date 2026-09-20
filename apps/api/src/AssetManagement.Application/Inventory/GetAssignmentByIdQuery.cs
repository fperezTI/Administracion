using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

public sealed record GetAssignmentByIdQuery(Guid AssignmentId) : IRequest<AssignmentDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assignments.Read;
}

public sealed record SignatureInfo(string SignerDisplayName, DateTimeOffset SignedAtUtc, string ContentHash);

public sealed record AssignmentDetail(
    Guid Id,
    Guid AssetId,
    string AssetFolio,
    Guid AssignedToUserId,
    string AssignedToDisplayName,
    Guid? OrgUnitId,
    AssignmentStatus Status,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? ReturnedAtUtc,
    SignatureInfo? AcceptanceSignature,
    SignatureInfo? ReturnSignature);

public sealed class GetAssignmentByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAssignmentByIdQuery, AssignmentDetail>
{
    public async Task<AssignmentDetail> Handle(GetAssignmentByIdQuery request, CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Assignment), request.AssignmentId);

        var asset = await db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == assignment.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Assets.Asset), assignment.AssetId);

        var recipient = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == assignment.AssignedToUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Identity.User), assignment.AssignedToUserId);

        SignatureInfo? acceptanceSignature = null;
        if (assignment.SignatureRecordId is { } acceptanceId)
        {
            var record = await db.SignatureRecords.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == acceptanceId, cancellationToken);
            if (record is not null)
            {
                acceptanceSignature = new SignatureInfo(record.SignerDisplayName, record.SignedAtUtc, record.ContentHash);
            }
        }

        SignatureInfo? returnSignature = null;
        if (assignment.ReturnSignatureRecordId is { } returnId)
        {
            var record = await db.SignatureRecords.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == returnId, cancellationToken);
            if (record is not null)
            {
                returnSignature = new SignatureInfo(record.SignerDisplayName, record.SignedAtUtc, record.ContentHash);
            }
        }

        return new AssignmentDetail(
            assignment.Id, assignment.AssetId, asset.InternalFolio, assignment.AssignedToUserId, recipient.DisplayName,
            assignment.OrgUnitId, assignment.Status, assignment.AssignedAtUtc, assignment.AcceptedAtUtc,
            assignment.ReturnedAtUtc, acceptanceSignature, returnSignature);
    }
}
