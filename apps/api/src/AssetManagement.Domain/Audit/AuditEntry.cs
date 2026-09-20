using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Audit;

/// <summary>
/// One immutable audit trail row (pedido §11: "tabla AuditEntry de solo inserción"), written by
/// <c>AuditBehavior</c> (Application) for every command marked <c>IAuditableCommand</c>. Deliberately not
/// a before/after field-level diff with per-field redaction — see the F8 plan decision 3: this is a real,
/// queryable "who did what when" trail (command name, its parameters as submitted, outcome), not a
/// generic reflection-based snapshot+redaction engine, which the pedido mentions but does not detail
/// further. <see cref="UserDisplayName"/> is denormalized on purpose: if the user is ever removed, the
/// historical trail must not lose who acted.
/// </summary>
public sealed class AuditEntry : AggregateRoot<Guid>
{
    public Guid? CompanyId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? UserDisplayName { get; private set; }
    public string CommandName { get; private set; } = null!;
    public string? Module { get; private set; }
    public string? Action { get; private set; }
    public string? DetailsJson { get; private set; }
    public bool Succeeded { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    private AuditEntry()
    {
    }

    private AuditEntry(
        Guid id, Guid? companyId, Guid? userId, string? userDisplayName, string commandName, string? module,
        string? action, string? detailsJson, bool succeeded, string? errorMessage, string? ipAddress,
        string? userAgent, string? correlationId, DateTimeOffset nowUtc)
        : base(id)
    {
        CompanyId = companyId;
        UserId = userId;
        UserDisplayName = userDisplayName;
        CommandName = commandName;
        Module = module;
        Action = action;
        DetailsJson = detailsJson;
        Succeeded = succeeded;
        ErrorMessage = errorMessage;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CorrelationId = correlationId;
        OccurredAtUtc = nowUtc;
    }

    public static AuditEntry Create(
        Guid? companyId, Guid? userId, string? userDisplayName, string commandName, string? module, string? action,
        string? detailsJson, bool succeeded, string? errorMessage, string? ipAddress, string? userAgent,
        string? correlationId, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            throw new DomainException("El nombre del comando auditado es obligatorio.");
        }

        return new AuditEntry(
            Guid.NewGuid(), companyId, userId, userDisplayName, commandName.Trim(), module, action, detailsJson,
            succeeded, errorMessage, ipAddress, userAgent, correlationId, nowUtc);
    }
}
