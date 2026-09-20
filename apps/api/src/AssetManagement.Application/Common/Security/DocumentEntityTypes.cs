namespace AssetManagement.Application.Common.Security;

/// <summary>Allowlist of entity types a <see cref="AssetManagement.Domain.Documents.Document"/> may attach
/// to (pedido: "asociación polimórfica controlada"). Add a constant here — and a lookup case in
/// <c>UploadDocumentCommand</c>/<c>GetDocumentContentQuery</c> — when a future phase needs to attach
/// evidence to another entity; V1 covers the two places the pedido already calls out "evidencias" for
/// (see the F8 plan).</summary>
public static class DocumentEntityTypes
{
    public const string Asset = "Asset";
    public const string MaintenanceOrder = "MaintenanceOrder";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Asset, MaintenanceOrder };
}
