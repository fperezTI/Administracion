using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>
/// Self-service rejection of a pending assignment — the mirror image of <see cref="SignAssignmentCommand"/>:
/// deliberately does not implement IRequiresPermission, only an ownership check (anyone can reject, but
/// only their own assignment). Has exactly the same real-world effect as an administrator's
/// <see cref="CancelPendingAssignmentCommand"/> (the asset returns to the warehouse), just triggered by the
/// recipient instead, with no reason required — a one-click action, symmetric with "Aceptar".
/// </summary>
public sealed record RejectAssignmentCommand(Guid AssignmentId) : IRequest;

public sealed class RejectAssignmentCommandHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock,
    INotificationSender notificationSender, IFrontendLinkBuilder linkBuilder)
    : IRequestHandler<RejectAssignmentCommand>
{
    public async Task Handle(RejectAssignmentCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión para rechazar una asignación.");
        }

        var assignment = await db.Assignments.FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Assignment), request.AssignmentId);

        if (assignment.AssignedToUserId != userId)
        {
            throw new ForbiddenAccessException("Solo el destinatario de la asignación puede rechazarla.");
        }

        var now = clock.UtcNow;

        var groupMembers = await AssignmentGroupSupport.GetGroupMembersAsync(
            db, assignment, AssignmentStatus.PendingSignature, cancellationToken);

        var rejectedAssets = new List<Asset>();
        foreach (var member in groupMembers)
        {
            var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == member.AssetId, cancellationToken)
                ?? throw new NotFoundException(nameof(Asset), member.AssetId);

            var movement = await db.Movements.FirstOrDefaultAsync(m => m.Id == member.MovementId, cancellationToken)
                ?? throw new NotFoundException(nameof(Movement), member.MovementId);

            member.Cancel(now, userId);
            movement.Cancel(now, userId);
            asset.ChangeStatus(AssetStatus.InWarehouse, now, userId);
            rejectedAssets.Add(asset);
        }

        await db.SaveChangesAsync(cancellationToken);

        // CreatedByUserId can be null in theory (AuditableAggregateRoot allows it) — nothing to notify then.
        if (assignment.CreatedByUserId is { } createdByUserId)
        {
            var link = linkBuilder.AssignmentUrl(assignment.Id);
            await notificationSender.NotifyAsync(
                createdByUserId, "AssignmentRejected", "Rechazaron una asignación",
                BuildPlainTextBody(currentUser.DisplayName, rejectedAssets, link), assignment.CompanyId, cancellationToken,
                emailBodyHtml: BuildHtmlBody(currentUser.DisplayName, rejectedAssets, link));
        }
    }

    private static string BuildPlainTextBody(string? rejectedByDisplayName, IReadOnlyList<Asset> assets, string link)
    {
        var who = rejectedByDisplayName ?? "El destinatario";
        var items = string.Join(", ", assets.Select(a => $"{a.InternalFolio} ({a.Brand} {a.Model})"));
        return $"{who} rechazó la asignación de: {items}. Revisa {link}.";
    }

    private static string BuildHtmlBody(string? rejectedByDisplayName, IReadOnlyList<Asset> assets, string link)
    {
        var who = System.Net.WebUtility.HtmlEncode(rejectedByDisplayName ?? "El destinatario");
        var rows = string.Join("", assets.Select(a =>
            $"<tr><td style=\"padding:4px 12px;border:1px solid #ddd;font-family:monospace\">{System.Net.WebUtility.HtmlEncode(a.InternalFolio)}</td>" +
            $"<td style=\"padding:4px 12px;border:1px solid #ddd\">{System.Net.WebUtility.HtmlEncode(a.Brand)} {System.Net.WebUtility.HtmlEncode(a.Model)}</td></tr>"));

        return $"""
            <p><strong>{who}</strong> rechazó la asignación del siguiente equipo:</p>
            <table style="border-collapse:collapse;margin:12px 0">
              <thead><tr>
                <th style="padding:4px 12px;border:1px solid #ddd;text-align:left">Folio</th>
                <th style="padding:4px 12px;border:1px solid #ddd;text-align:left">Marca / modelo</th>
              </tr></thead>
              <tbody>{rows}</tbody>
            </table>
            <p>
              <a href="{link}" style="display:inline-block;padding:10px 20px;background:#1a56db;color:#fff;text-decoration:none;border-radius:6px">
                Ver asignación
              </a>
            </p>
            <p style="color:#666;font-size:12px">Si el botón no funciona, copia y pega este enlace: {link}</p>
            """;
    }
}
