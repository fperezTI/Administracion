import { redirect } from "next/navigation";
import { auth } from "@/auth";

/**
 * middleware.ts already redirects unauthenticated requests away from every route that calls this —
 * this is the defensive fallback so Server Components never proceed with a missing token, and so
 * TypeScript narrows `accessToken` to `string` for the caller.
 *
 * Also treats a failed silent refresh (`session.error === "RefreshAccessTokenError"`, set in
 * src/auth.ts when the Entra ID refresh_token grant fails — expired/revoked refresh token) as
 * equivalent to no session: forwarding a stale, soon-to-be-rejected access token to the API would
 * just surface as a confusing 401 deep in a page instead of a clean re-login prompt.
 */
export async function requireAccessToken(): Promise<string> {
  const session = await auth();
  if (!session?.accessToken || session.error === "RefreshAccessTokenError") {
    redirect("/");
  }
  return session.accessToken;
}
