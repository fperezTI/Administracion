import { cache } from "react";
import NextAuth from "next-auth";
import MicrosoftEntraID from "next-auth/providers/microsoft-entra-id";
import { getMe } from "@/lib/api";

/**
 * Server-side only. The API access token issued here never reaches client-side JavaScript: it lives
 * in the encrypted, httpOnly session cookie and is read back only from Server Components and Route
 * Handlers (see docs/security/authentication.md — this is the BFF pattern the architecture calls for).
 * No Client Component in this app calls useSession()/SessionProvider, which is what would otherwise
 * serialize this token to the browser.
 */
const apiScope = `api://${process.env.ENTRA_API_CLIENT_ID}/access_as_user`;

const nextAuth = NextAuth({
  trustHost: true,
  // debug:true was used only to diagnose the initial setup (see docs/security/authentication.md) — it
  // dumps full provider config, including the client secret, and decoded token claims to stdout. Never
  // enable it beyond a one-off local troubleshooting session.
  logger: {
    error(error: Error) {
      console.error("[auth][error]", error.message);
    },
  },
  providers: [
    MicrosoftEntraID({
      clientId: process.env.ENTRA_CLIENT_ID,
      clientSecret: process.env.ENTRA_CLIENT_SECRET,
      issuer: `https://login.microsoftonline.com/${process.env.ENTRA_TENANT_ID}/v2.0`,
      authorization: {
        params: { scope: `openid profile email offline_access ${apiScope}` },
      },
    }),
  ],
  callbacks: {
    async jwt({ token, account, trigger, session }) {
      if (account) {
        // First sign-in: Entra ID just issued tokens for the requested scope. Seed the effective
        // theme too, so the very first authenticated page already renders it correctly.
        return withEffectiveTheme({
          ...token,
          accessToken: account.access_token,
          refreshToken: account.refresh_token,
          accessTokenExpiresAt: (account.expires_at ?? 0) * 1000,
        });
      }

      // Triggered by unstable_update() from the theme-preference Server Action — see
      // lib/theme-actions.ts. Lets a confirmed theme change apply immediately instead of waiting
      // for the next access-token refresh.
      if (trigger === "update" && typeof session?.effectiveTheme === "string") {
        return { ...token, effectiveTheme: session.effectiveTheme, themePreference: session.themePreference ?? null };
      }

      if (Date.now() < (token.accessTokenExpiresAt as number)) {
        return token;
      }

      return refreshAccessToken(token);
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken as string | undefined;
      session.error = token.error as string | undefined;
      session.effectiveTheme = token.effectiveTheme as string | undefined;
      session.themePreference = token.themePreference as string | null | undefined;
      return session;
    },
  },
});

export const { handlers, auth, signIn, signOut, unstable_update } = nextAuth;

/** Same session read as `auth()`, memoized per request (React.cache) — every shelled page now
 * reads the session at least twice (Topbar + the page's own requireAccessToken()), and each read
 * otherwise independently decrypts/verifies the session JWT and, if the access token is near
 * expiry, calls refreshAccessToken() — duplicating a real network round-trip to Entra ID. */
export const getSession = cache(async () => nextAuth.auth());

async function refreshAccessToken(token: Record<string, unknown>) {
  try {
    const tenantId = process.env.ENTRA_TENANT_ID;
    const response = await fetch(`https://login.microsoftonline.com/${tenantId}/oauth2/v2.0/token`, {
      method: "POST",
      headers: { "Content-Type": "application/x-www-form-urlencoded" },
      body: new URLSearchParams({
        client_id: process.env.ENTRA_CLIENT_ID!,
        client_secret: process.env.ENTRA_CLIENT_SECRET!,
        grant_type: "refresh_token",
        refresh_token: token.refreshToken as string,
        scope: `openid profile email offline_access ${apiScope}`,
      }),
    });

    const refreshed = await response.json();
    if (!response.ok) {
      throw new Error(refreshed.error_description ?? "Failed to refresh the access token.");
    }

    return withEffectiveTheme({
      ...token,
      accessToken: refreshed.access_token,
      refreshToken: refreshed.refresh_token ?? token.refreshToken,
      accessTokenExpiresAt: Date.now() + refreshed.expires_in * 1000,
      error: undefined,
    });
  } catch (error) {
    console.error("Failed to refresh Entra ID access token", error);
    return { ...token, error: "RefreshAccessTokenError" };
  }
}

/** Refreshes the cached effective theme (personal preference, or the active company's default) as a
 * side effect of sign-in and of each access-token refresh — this is what lets a Super Administrador
 * changing a company's default theme reach users in "usar tema de la empresa" mode "en la siguiente
 * carga" (pedido) without a dedicated cookie or an extra call on every navigation. Never throws: a
 * failed lookup just leaves effectiveTheme as-is, and the root layout falls back safely either way. */
async function withEffectiveTheme(token: Record<string, unknown>) {
  try {
    const me = await getMe(token.accessToken as string);
    return { ...token, effectiveTheme: me.effectiveTheme, themePreference: me.themePreference };
  } catch (error) {
    console.error("Failed to resolve the effective theme", error);
    return token;
  }
}
