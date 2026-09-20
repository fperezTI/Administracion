import { auth } from "@/auth";

/**
 * Proxies a document's bytes from the .NET API, which is only reachable server-side
 * (API_INTERNAL_URL, never exposed to the browser — see lib/api.ts) — the browser can't fetch it
 * directly, and a plain `<a href>` link can't attach an Authorization header anyway. Streams the
 * response through rather than buffering it, and re-validates the session on every request (same
 * permission/company check the API itself performs — see GetDocumentContentQuery).
 */
export async function GET(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const session = await auth();
  if (!session?.accessToken || session.error === "RefreshAccessTokenError") {
    return new Response(null, { status: 401 });
  }

  const { id } = await params;
  const apiBaseUrl = process.env.API_INTERNAL_URL ?? "http://localhost:5080";

  const response = await fetch(`${apiBaseUrl}/api/v1/documents/${id}/content`, {
    headers: { Authorization: `Bearer ${session.accessToken}` },
    cache: "no-store",
  });

  if (!response.ok || !response.body) {
    return new Response(null, { status: response.status });
  }

  return new Response(response.body, {
    status: 200,
    headers: {
      "Content-Type": response.headers.get("Content-Type") ?? "application/octet-stream",
      "Content-Disposition": response.headers.get("Content-Disposition") ?? "attachment",
    },
  });
}
