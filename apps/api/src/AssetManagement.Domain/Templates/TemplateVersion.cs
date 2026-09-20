namespace AssetManagement.Domain.Templates;

/// <summary>One immutable revision of a <see cref="Template"/>'s content. "La versión usada en una firma o
/// documento es inmutable" (domain-model.md) — content is never edited after creation, only superseded by
/// a new version via <see cref="Template.AddVersion"/>.</summary>
public sealed class TemplateVersion
{
    public Guid TemplateId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Content { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    private TemplateVersion()
    {
    }

    internal static TemplateVersion Create(
        Guid templateId, int versionNumber, string content, DateTimeOffset nowUtc, Guid? createdByUserId) =>
        new()
        {
            TemplateId = templateId,
            VersionNumber = versionNumber,
            Content = content,
            CreatedAtUtc = nowUtc,
            CreatedByUserId = createdByUserId,
        };
}
