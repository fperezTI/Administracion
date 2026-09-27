namespace AssetManagement.Domain.Theming;

/// <summary>
/// The five predefined visual themes (pedido: sistema de temas visuales) plus the safe fallback used
/// when a stored/incoming code is null, unknown or otherwise invalid. Themes are defined entirely in
/// code (CSS custom properties + a typed metadata registry on the frontend) — this whitelist is the
/// only thing the backend needs to know about them: which codes are valid, for validating
/// <see cref="AssetManagement.Domain.Identity.User.ThemePreferenceCode"/> and
/// <see cref="AssetManagement.Domain.Organization.Company.DefaultThemeCode"/>. No display name,
/// description or color lives here — those are UI concerns owned by next-intl/the frontend registry.
/// </summary>
public static class ThemeCode
{
    public const string Light = "light";
    public const string Dark = "dark";
    public const string CorporateBlue = "corporate-blue";
    public const string ExecutiveGray = "executive-gray";
    public const string HighContrast = "high-contrast";

    /// <summary>Used whenever a user has no personal preference and the active company's own default is
    /// itself null, invalid or unknown — never left unresolved.</summary>
    public const string Fallback = Light;

    public static readonly IReadOnlyCollection<string> All =
        [Light, Dark, CorporateBlue, ExecutiveGray, HighContrast];

    public static bool IsValid(string? code) => code is not null && All.Contains(code);

    /// <summary>Returns <paramref name="code"/> unchanged if valid, otherwise <see cref="Fallback"/> —
    /// used when resolving a company's effective default, which must always be a concrete theme (never
    /// null, unlike a user's personal preference).</summary>
    public static string OrFallback(string? code) => IsValid(code) ? code! : Fallback;
}
