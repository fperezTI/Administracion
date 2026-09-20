using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Notifications;

/// <summary>
/// One in-app notification for one user (pedido: "notificaciones in-app + correo"). Deliberately not an
/// <see cref="AuditableAggregateRoot{TId}"/> — it is ephemeral, self-service data the recipient owns and
/// marks read themselves, not an audited record. Email delivery (best-effort, see
/// <c>INotificationSender</c>/<c>IEmailSender</c> in Application/Infrastructure) is not tracked here as a
/// separate delivery-status/retry state — V1 simplification, see the F8 plan decision 2.
/// </summary>
public sealed class Notification : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public Guid? CompanyId { get; private set; }
    public string Type { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    private Notification()
    {
    }

    private Notification(
        Guid id, Guid userId, Guid? companyId, string type, string title, string body, DateTimeOffset nowUtc)
        : base(id)
    {
        UserId = userId;
        CompanyId = companyId;
        Type = type;
        Title = title;
        Body = body;
        IsRead = false;
        CreatedAtUtc = nowUtc;
    }

    public static Notification Create(Guid userId, string type, string title, string body, Guid? companyId, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new DomainException("El tipo de notificación es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("El título de la notificación es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException("El contenido de la notificación es obligatorio.");
        }

        return new Notification(Guid.NewGuid(), userId, companyId, type.Trim(), title.Trim(), body.Trim(), nowUtc);
    }

    public void MarkAsRead(DateTimeOffset nowUtc)
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = nowUtc;
    }
}
