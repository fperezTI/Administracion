/** The five predefined visual themes plus the safe fallback — mirrors the backend's whitelist
 * (apps/api/src/AssetManagement.Domain/Theming/ThemeCode.cs) exactly. Themes are defined entirely in
 * code: this registry is the single source of truth for which codes exist and how they map to a CSS
 * class on <html> (see app/globals.css for the actual color tokens per class) and to a Logomark
 * rendering variant. Deliberately NOT fetched from the backend — the frontend needs this typed
 * mapping regardless, and the backend only needs to validate codes against its own copy of the same
 * list, so a network round-trip here would be pure duplication. Display strings live in
 * messages/es.json under "Theme", keyed by these same codes — never hardcoded here. */
export const THEME_CODES = ["light", "dark", "corporate-blue", "executive-gray", "high-contrast"] as const;

export type ThemeCode = (typeof THEME_CODES)[number];

/** "Usar tema de la empresa" isn't a visual theme — it's the absence of a personal preference
 * (ThemePreference === null), which the frontend selector represents with this sentinel value so it
 * can share a single radio/select control with the five real themes. Never sent to the backend as a
 * themeCode — the Server Action maps it back to `null` before calling the API. */
export const INHERIT_COMPANY_THEME = "company" as const;

export type ThemeSelection = ThemeCode | typeof INHERIT_COMPANY_THEME;

export const FALLBACK_THEME: ThemeCode = "light";

export function isThemeCode(value: string | null | undefined): value is ThemeCode {
  return value != null && (THEME_CODES as readonly string[]).includes(value);
}

/** Never trust a theme code from a cookie, a stale session, or old data — always pass it through
 * this before applying it to the DOM. */
export function resolveThemeCode(value: string | null | undefined): ThemeCode {
  return isThemeCode(value) ? value : FALLBACK_THEME;
}
