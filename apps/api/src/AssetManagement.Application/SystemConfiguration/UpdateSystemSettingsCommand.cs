using System.Text.Json;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Configuration;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.SystemConfiguration;

/// <summary>
/// GraphClientSecret is write-only: null/empty means "leave the stored secret unchanged" (the frontend
/// never pre-fills it). It deliberately is NOT [JsonIgnore] — that attribute applies symmetrically to both
/// serialization AND deserialization in System.Text.Json, which would silently drop the value during
/// ASP.NET Core's model binding too, not just when audited. Instead, IRedactsAuditDetails gives
/// AuditBehavior a safe stand-in so the plaintext value never reaches AuditEntry.DetailsJson, which is
/// readable by anyone with the broader Audit.Read permission (CLAUDE.md regla 5).
/// </summary>
public sealed record UpdateSystemSettingsCommand(
    string? SenderMailbox,
    string? GraphTenantId,
    string? GraphClientId,
    string? GraphClientSecret) : IRequest, IRequiresPermission, IAuditableCommand, IRedactsAuditDetails
{
    public string PermissionCode => PermissionCatalog.Configuration.Update;

    public string ToRedactedAuditJson() => JsonSerializer.Serialize(new
    {
        SenderMailbox,
        GraphTenantId,
        GraphClientId,
        GraphClientSecretProvided = !string.IsNullOrWhiteSpace(GraphClientSecret),
        PermissionCode,
    });
}

public sealed class UpdateSystemSettingsCommandValidator : AbstractValidator<UpdateSystemSettingsCommand>
{
    public UpdateSystemSettingsCommandValidator()
    {
        RuleFor(x => x.SenderMailbox).MaximumLength(320);
        RuleFor(x => x.GraphTenantId).MaximumLength(64);
        RuleFor(x => x.GraphClientId).MaximumLength(64);
        RuleFor(x => x.GraphClientSecret).MaximumLength(1000);
    }
}

public sealed class UpdateSystemSettingsCommandHandler(
    IApplicationDbContext db, ISecretProtector secretProtector, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<UpdateSystemSettingsCommand>
{
    public async Task Handle(UpdateSystemSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings.FirstOrDefaultAsync(
            s => s.Id == SystemSettings.SingletonId, cancellationToken);
        var isNew = settings is null;
        settings ??= SystemSettings.CreateEmpty(clock.UtcNow);

        var graphClientSecretCiphertext = string.IsNullOrWhiteSpace(request.GraphClientSecret)
            ? settings.GraphClientSecretCiphertext
            : secretProtector.Protect(request.GraphClientSecret);

        settings.Update(
            request.SenderMailbox, request.GraphTenantId, request.GraphClientId, graphClientSecretCiphertext,
            currentUser.UserId, clock.UtcNow);

        if (isNew)
        {
            db.SystemSettings.Add(settings);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
