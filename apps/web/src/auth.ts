import NextAuth from "next-auth";
import MicrosoftEntraID from "next-auth/providers/microsoft-entra-id";

/**
 * Server-side only. The API access token issued here never reaches client-side JavaScript: it lives
 * in the encrypted, httpOnly session cookie and is read back only from Server Components and Route
 * Handlers (see docs/security/authentication.md — this is the BFF pattern the architecture calls for).
 * No Client Component in this app calls useSession()/SessionProvider, which is what would otherwise
 * serialize this token to the browser.
 */
const apiScope = `api://${process.env.ENTRA_API_CLIENT_ID}/access_as_user`;

export const { handlers, auth, signIn, signOut } = NextAuth({
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
    async jwt({ token, account }) {
      if (account) {
        // First sign-in: Entra ID just issued tokens for the requested scope.
        return {
          ...token,
          accessToken: account.access_token,
          refreshToken: account.refresh_token,
          accessTokenExpiresAt: (account.expires_at ?? 0) * 1000,
        };
      }

      if (Date.now() < (token.accessTokenExpiresAt as number)) {
        return token;
      }

      return refreshAccessToken(token);
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken as string | undefined;
      session.error = token.error as string | undefined;
      return session;
    },
  },
});

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

    return {
      ...token,
      accessToken: refreshed.access_token,
      refreshToken: refreshed.refresh_token ?? token.refreshToken,
      accessTokenExpiresAt: Date.now() + refreshed.expires_in * 1000,
      error: undefined,
    };
  } catch (error) {
    console.error("Failed to refresh Entra ID access token", error);
    return { ...token, error: "RefreshAccessTokenError" };
  }
}
