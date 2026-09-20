using System.Reflection;
using System.Text.Json;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Audit;
using MediatR;

namespace AssetManagement.Application.Common.Behaviors;

/// <summary>
/// Writes an <see cref="AuditEntry"/> for every <see cref="IAuditableCommand"/>, success or failure —
/// registered after <see cref="ValidationBehavior{TRequest,TResponse}"/> so only attempts that already
/// passed authorization/validation reach the audit trail (permission/validation failures belong to the
/// technical Serilog log via <see cref="LoggingBehavior{TRequest,TResponse}"/>, not the functional audit
/// trail — CLAUDE.md regla 5). Writes in its own <c>SaveChangesAsync</c>, independent of whatever the
/// handler itself saved (or failed to).
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IAuditableCommand)
        {
            return await next();
        }

        string? detailsJson;
        try
        {
            detailsJson = JsonSerializer.Serialize(request, request.GetType(), SerializerOptions);
        }
        catch (NotSupportedException)
        {
            detailsJson = null;
        }

        string? module = null;
        string? action = null;
        if (request is IRequiresPermission requiresPermission)
        {
            var parts = requiresPermission.PermissionCode.Split('.', 2);
            module = parts.ElementAtOrDefault(0);
            action = parts.ElementAtOrDefault(1);
        }

        // Best-effort only, not a security boundary (the global query filter never scoped AuditEntry by
        // company — see AppDbContext) — many commands carry a CompanyId property directly (CreateAssetCommand,
        // CreateConsumableCommand, ...); others only reference an AssetId/RoleId/etc. and simply record null
        // here, which is fine since this is purely a display/filter convenience for the audit panel.
        var companyId = TryGetCompanyId(request);

        try
        {
            var response = await next();
            await RecordAsync(companyId, module, action, detailsJson, succeeded: true, errorMessage: null, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            await RecordAsync(companyId, module, action, detailsJson, succeeded: false, ex.Message, cancellationToken);
            throw;
        }
    }

    private static Guid? TryGetCompanyId(TRequest request)
    {
        var property = typeof(TRequest).GetProperty("CompanyId", BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(request) as Guid?;
    }

    private async Task RecordAsync(
        Guid? companyId, string? module, string? action, string? detailsJson, bool succeeded, string? errorMessage,
        CancellationToken cancellationToken)
    {
        var entry = AuditEntry.Create(
            companyId, currentUser.UserId, currentUser.DisplayName, typeof(TRequest).Name, module, action,
            detailsJson, succeeded, errorMessage, currentUser.IpAddress, currentUser.UserAgent, currentUser.CorrelationId,
            clock.UtcNow);

        db.AuditEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
    }
}
