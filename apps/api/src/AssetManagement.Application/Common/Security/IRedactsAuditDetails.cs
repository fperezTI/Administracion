namespace AssetManagement.Application.Common.Security;

/// <summary>
/// A command that carries a value too sensitive to ever appear in AuditEntry.DetailsJson (e.g. a real
/// external credential like a Graph client secret) implements this to provide its own redacted JSON
/// instead of AuditBehavior's default reflection-based `JsonSerializer.Serialize(request, ...)`.
///
/// Note: the sensitive property must NOT be marked [JsonIgnore] to solve this — System.Text.Json applies
/// that attribute symmetrically to both serialization AND deserialization, which would silently drop the
/// value during ASP.NET Core's model binding too (the bug this interface exists to avoid).
/// </summary>
public interface IRedactsAuditDetails
{
    string ToRedactedAuditJson();
}
