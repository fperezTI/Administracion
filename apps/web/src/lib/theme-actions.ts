"use server";

import { unstable_update } from "@/auth";
import { requireAccessToken } from "@/lib/require-session";
import { getMe, setMyThemePreference } from "@/lib/api";
import { FALLBACK_THEME, INHERIT_COMPANY_THEME, resolveThemeCode, type ThemeSelection } from "@/lib/theme-catalog";

export type ConfirmThemeResult = { effectiveTheme: string } | { error: string };

/** Lets the selector preview "usar tema de la empresa" accurately even when the user currently has an
 * explicit personal preference. Deliberately reads the active company's own `defaultThemeCode` from
 * `me.companies` instead of `me.effectiveTheme` — the latter is dominated by the *current* personal
 * preference (still persisted at preview time, since a preview never mutates it), so it would just
 * echo back whatever theme is already applied instead of showing what inheriting would look like.
 * Falls back to the first accessible company, mirroring GetMeQueryHandler's own fallback for when no
 * active company is explicitly selected (see docs/multi-company.md — no frontend caller sends
 * X-Active-Company-Id yet). Read-only, called only when this specific option is opened in the
 * gallery — not on every render. */
export async function previewCompanyThemeAction(): Promise<string> {
  const accessToken = await requireAccessToken();
  const me = await getMe(accessToken);
  const activeCompany = me.companies.find((c) => c.companyId === me.activeCompanyId) ?? me.companies[0];
  return activeCompany?.defaultThemeCode ?? FALLBACK_THEME;
}

/** Persists the user's confirmed theme choice (see the selector's preview/confirm/timer flow in
 * user-menu-theme-selector.tsx) and immediately refreshes the cached effective theme in the session
 * JWT via unstable_update() — so the very next render already reflects it, without waiting for the
 * next access-token refresh. Never accepts a target user id: requireAccessToken() only ever resolves
 * the caller's own session. */
export async function confirmThemeSelectionAction(selection: ThemeSelection): Promise<ConfirmThemeResult> {
  const accessToken = await requireAccessToken();
  const themeCode = selection === INHERIT_COMPANY_THEME ? null : selection;

  try {
    await setMyThemePreference(accessToken, themeCode);

    // Setting a concrete personal theme: the effective theme *is* that code, no extra round trip.
    // Restoring company inheritance: we don't know the company's current default from here, so ask.
    const effectiveTheme = themeCode ?? (await getMe(accessToken)).effectiveTheme;

    await unstable_update({ effectiveTheme, themePreference: themeCode });

    return { effectiveTheme: resolveThemeCode(effectiveTheme) };
  } catch {
    return { error: "No fue posible guardar tu preferencia de tema." };
  }
}
