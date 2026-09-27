import type { DefaultSession } from "next-auth";

declare module "next-auth" {
  interface Session {
    accessToken?: string;
    error?: string;
    user?: DefaultSession["user"];
    /** The caller's resolved theme (personal preference, or the active company's default) — cached
     * in the session JWT so the root layout can render it without a fresh backend call on every
     * navigation. Refreshed on sign-in, on access-token refresh, and explicitly via
     * unstable_update() when the user confirms a new preference. See src/auth.ts. */
    effectiveTheme?: string;
    /** Null (or absent) means "usar tema de la empresa" — see effectiveTheme for what applies. */
    themePreference?: string | null;
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    accessToken?: string;
    refreshToken?: string;
    accessTokenExpiresAt?: number;
    error?: string;
    effectiveTheme?: string;
    themePreference?: string | null;
  }
}
