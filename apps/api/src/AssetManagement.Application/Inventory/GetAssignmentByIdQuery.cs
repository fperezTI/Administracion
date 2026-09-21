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

public sealed record AssignmentGroupMember(
    Guid AssetId,
    string AssetFolio,
    string? PatrimonialFolio,
    string Brand,
    string Model,
    string? SerialNumber,
    bool IsPrimary);

public sealed record AssignmentDetail(
    Guid Id,
    Guid CompanyId,
    Guid AssetId,
    string AssetFolio,
    string? AssetPatrimonialFolio,
    string AssetBrand,
    string AssetModel,
    string? AssetSerialNumber,
    string? AssetDescription,
    Guid AssignedToUserId,
    string AssignedToDisplayName,
    string AssignedToEmail,
    Guid? OrgUnitId,
    string? OrgUnitName,
    string MovementFolio,
    AssignmentStatus Status,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? ReturnedAtUtc,
    SignatureInfo? AcceptanceSignature,
    SignatureInfo? ReturnSignature,
    IReadOnlyList<AssignmentGroupMember> GroupMembers);

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

        var movement = await db.Movements.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == assignment.MovementId, cancellationToken)
            ?? throw new NotFoundException(nameof(Movement), assignment.MovementId);

        string? orgUnitName = null;
        if (assignment.OrgUnitId is { } orgUnitId)
        {
            orgUnitName = await db.OrgUnits.AsNoTracking()
                .Where(o => o.Id == orgUnitId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

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

        IReadOnlyList<AssignmentGroupMember> groupMembers;
        if (assignment.AssignmentGroupId is { } groupId)
        {
            groupMembers = await (
                from sibling in db.Assignments.AsNoTracking()
                where sibling.AssignmentGroupId == groupId
                join siblingAsset in db.Assets.AsNoTracking() on sibling.AssetId equals siblingAsset.Id
                select new AssignmentGroupMember(
                    siblingAsset.Id, siblingAsset.InternalFolio, siblingAsset.PatrimonialFolio, siblingAsset.Brand,
                    siblingAsset.Model, siblingAsset.SerialNumber, siblingAsset.AccessoryOfAssetId == null))
                .ToListAsync(cancellationToken);
        }
        else
        {
            groupMembers =
            [
                new AssignmentGroupMember(
                    asset.Id, asset.InternalFolio, asset.PatrimonialFolio, asset.Brand, asset.Model,
                    asset.SerialNumber, asset.AccessoryOfAssetId == null),
            ];
        }

        return new AssignmentDetail(
            assignment.Id, assignment.CompanyId, assignment.AssetId, asset.InternalFolio, asset.PatrimonialFolio,
            asset.Brand, asset.Model, asset.SerialNumber, asset.Description, assignment.AssignedToUserId,
            recipient.DisplayName, recipient.Email, assignment.OrgUnitId, orgUnitName, movement.FolioNumber,
            assignment.Status, assignment.AssignedAtUtc, assignment.AcceptedAtUtc, assignment.ReturnedAtUtc,
            acceptanceSignature, returnSignature, groupMembers.OrderByDescending(m => m.IsPrimary).ToList());
    }
}
