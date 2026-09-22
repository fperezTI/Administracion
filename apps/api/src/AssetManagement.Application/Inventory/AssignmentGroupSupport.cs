using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Signature;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>
/// Shared cascade logic for "a primary asset plus the accessories assigned together with it" (e.g. a
/// laptop and its charger) — used by <see cref="CreateAssignmentCommand"/>, <see cref="ReassignAssetCommand"/>,
/// <see cref="SignAssignmentCommand"/>, <see cref="ReturnAssignmentCommand"/> and
/// <see cref="CancelPendingAssignmentCommand"/> so the "operate on every assignment sharing a
/// AssignmentGroupId" logic exists exactly once. A null AssignmentGroupId always means "just this one
/// assignment" — the common case, unaffected by any of this.
/// </summary>
internal static class AssignmentGroupSupport
{
    public static async Task<List<Asset>> ValidateAccessoriesAsync(
        IApplicationDbContext db, Asset primaryAsset, IReadOnlyList<Guid>? accessoryAssetIds,
        CancellationToken cancellationToken)
    {
        var ids = (accessoryAssetIds ?? []).Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var accessories = await db.Assets.Where(a => ids.Contains(a.Id)).ToListAsync(cancellationToken);
        var missingId = ids.FirstOrDefault(id => accessories.All(a => a.Id != id));
        if (missingId != Guid.Empty)
        {
            throw new NotFoundException(nameof(Asset), missingId);
        }

        foreach (var accessory in accessories)
        {
            if (accessory.AccessoryOfAssetId != primaryAsset.Id)
            {
                throw new ConflictException(
                    $"El activo {accessory.InternalFolio} no es un accesorio de {primaryAsset.InternalFolio}.");
            }

            if (accessory.Status != AssetStatus.InWarehouse)
            {
                throw new ConflictException($"El accesorio {accessory.InternalFolio} no está disponible en almacén.");
            }
        }

        return accessories;
    }

    /// <summary>Creates one Movement + Assignment per asset (primary first, then each accessory), all
    /// sharing a fresh AssignmentGroupId — or no group id at all when there are no accessories, so a
    /// plain single-asset assignment looks exactly like it did before this feature existed. Also notifies
    /// the recipient (in-app + best-effort email with a link to confirm) once per call — i.e. once per
    /// group, never once per asset — covering both a fresh CreateAssignmentCommand and the new group half
    /// of ReassignAssetCommand identically.</summary>
    public static async Task<(Domain.Inventory.Assignment PrimaryAssignment, Domain.Inventory.Movement PrimaryMovement)> CreateGroupAsync(
        IApplicationDbContext db, IFolioGenerator folioGenerator, INotificationSender notificationSender,
        IFrontendLinkBuilder linkBuilder, Asset primaryAsset, IReadOnlyList<Asset> accessories, Guid assignedToUserId,
        Guid? orgUnitId, string? notes, DateTimeOffset now, Guid? userId, CancellationToken cancellationToken)
    {
        Guid? groupId = accessories.Count > 0 ? Guid.NewGuid() : null;

        var primary = await CreateSingleAsync(
            db, folioGenerator, primaryAsset, assignedToUserId, orgUnitId, notes, groupId, now, userId, cancellationToken);

        foreach (var accessory in accessories)
        {
            await CreateSingleAsync(
                db, folioGenerator, accessory, assignedToUserId, orgUnitId, notes, groupId, now, userId, cancellationToken);
        }

        var link = linkBuilder.MyAssignmentUrl(primary.Assignment.Id);
        var allAssets = new List<Asset> { primaryAsset }.Concat(accessories).ToList();
        await notificationSender.NotifyAsync(
            assignedToUserId, "AssignmentPendingSignature", "Se te asignó un equipo",
            BuildPlainTextBody(allAssets, link), primaryAsset.CompanyId, cancellationToken,
            emailBodyHtml: BuildHtmlBody(allAssets, link));

        return primary;
    }

    private static string BuildPlainTextBody(IReadOnlyList<Asset> assets, string link)
    {
        var items = string.Join(", ", assets.Select(a => $"{a.InternalFolio} ({a.Brand} {a.Model})"));
        return $"Se te asignó: {items}. Ingresa a {link} para confirmar la recepción.";
    }

    private static string BuildHtmlBody(IReadOnlyList<Asset> assets, string link)
    {
        var rows = string.Join("", assets.Select(a =>
            $"<tr><td style=\"padding:4px 12px;border:1px solid #ddd;font-family:monospace\">{System.Net.WebUtility.HtmlEncode(a.InternalFolio)}</td>" +
            $"<td style=\"padding:4px 12px;border:1px solid #ddd\">{System.Net.WebUtility.HtmlEncode(a.Brand)} {System.Net.WebUtility.HtmlEncode(a.Model)}</td>" +
            $"<td style=\"padding:4px 12px;border:1px solid #ddd;font-family:monospace\">{System.Net.WebUtility.HtmlEncode(a.SerialNumber ?? "—")}</td></tr>"));

        return $"""
            <p>Se te asignó el siguiente equipo:</p>
            <table style="border-collapse:collapse;margin:12px 0">
              <thead><tr>
                <th style="padding:4px 12px;border:1px solid #ddd;text-align:left">Folio</th>
                <th style="padding:4px 12px;border:1px solid #ddd;text-align:left">Marca / modelo</th>
                <th style="padding:4px 12px;border:1px solid #ddd;text-align:left">Serie</th>
              </tr></thead>
              <tbody>{rows}</tbody>
            </table>
            <p>
              <a href="{link}" style="display:inline-block;padding:10px 20px;background:#1a56db;color:#fff;text-decoration:none;border-radius:6px">
                Ver y confirmar recepción
              </a>
            </p>
            <p style="color:#666;font-size:12px">Si el botón no funciona, copia y pega este enlace: {link}</p>
            """;
    }

    private static async Task<(Domain.Inventory.Assignment Assignment, Domain.Inventory.Movement Movement)> CreateSingleAsync(
        IApplicationDbContext db, IFolioGenerator folioGenerator, Asset asset, Guid assignedToUserId, Guid? orgUnitId,
        string? notes, Guid? groupId, DateTimeOffset now, Guid? userId, CancellationToken cancellationToken)
    {
        var folio = await folioGenerator.NextAsync(asset.CompanyId, FolioDocumentTypes.MovementAssignment, cancellationToken);

        var movement = Domain.Inventory.Movement.Create(
            asset.CompanyId, asset.Id, Domain.Inventory.MovementType.Assignment, folio, startsCompleted: false,
            fromOrgUnitId: asset.CurrentOrgUnitId, toOrgUnitId: orgUnitId, fromUserId: null, toUserId: assignedToUserId,
            notes: notes, now, userId);

        var assignment = Domain.Inventory.Assignment.Create(
            asset.CompanyId, asset.Id, assignedToUserId, orgUnitId, movement.Id, now, userId, groupId);

        asset.ChangeStatus(AssetStatus.Reserved, now, userId);

        db.Movements.Add(movement);
        db.Assignments.Add(assignment);

        return (assignment, movement);
    }

    /// <summary>Finds every assignment that should move together with the given one for a lifecycle step
    /// expecting a specific current status (PendingSignature to sign/cancel, Accepted to return) — just
    /// itself when it has no group.</summary>
    public static async Task<List<Domain.Inventory.Assignment>> GetGroupMembersAsync(
        IApplicationDbContext db, Domain.Inventory.Assignment assignment, Domain.Inventory.AssignmentStatus expectedStatus,
        CancellationToken cancellationToken)
    {
        if (assignment.AssignmentGroupId is not { } groupId)
        {
            return [assignment];
        }

        return await db.Assignments
            .Where(a => a.AssignmentGroupId == groupId && a.Status == expectedStatus)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Returns every assignment passed in — same steps as the original single-asset
    /// ReturnAssignmentCommand, once per asset, each with its own Movement/SignatureRecord.</summary>
    public static async Task ReturnGroupAsync(
        IApplicationDbContext db, IFolioGenerator folioGenerator, ICurrentUserContext currentUser,
        IReadOnlyList<Domain.Inventory.Assignment> assignments, string typedFullName, string? notes,
        DateTimeOffset now, Guid userId, CancellationToken cancellationToken)
    {
        foreach (var assignment in assignments)
        {
            var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assignment.AssetId, cancellationToken)
                ?? throw new NotFoundException(nameof(Asset), assignment.AssetId);

            var folio = await folioGenerator.NextAsync(
                assignment.CompanyId, FolioDocumentTypes.MovementAssignmentReturn, cancellationToken);
            var returnMovement = Domain.Inventory.Movement.Create(
                assignment.CompanyId, asset.Id, Domain.Inventory.MovementType.AssignmentReturn, folio, startsCompleted: true,
                fromOrgUnitId: asset.CurrentOrgUnitId, toOrgUnitId: null, fromUserId: assignment.AssignedToUserId,
                toUserId: null, notes: notes, now, userId);

            var payload = $"AssignmentReturn|{assignment.Id}|{asset.Id}|{userId}|{now:O}";
            var signature = SignatureRecord.Create(
                assignment.CompanyId, "AssignmentReturn", assignment.Id, userId, typedFullName.Trim(),
                currentUser.IpAddress, currentUser.UserAgent, SignatureHasher.Hash(payload),
                SignatureRecord.TypedConfirmationMechanism, null, now, userId);

            assignment.Return(signature.Id, returnMovement.Id, now, userId);
            asset.ChangeStatus(AssetStatus.InWarehouse, now, userId);

            db.Movements.Add(returnMovement);
            db.SignatureRecords.Add(signature);
        }
    }
}
