import { auth } from "@/auth";

/**
 * Proxies the CSV import template from the .NET API — same reasoning as
 * app/api/documents/[id]/content/route.ts (F8): the API is only reachable server-side, and a plain
 * `<a href>` can't attach an Authorization header.
 */
export async function GET(request: Request) {
  const session = await auth();
  if (!session?.accessToken || session.error === "RefreshAccessTokenError") {
    return new Response(null, { status: 401 });
  }

  const { searchParams } = new URL(request.url);
  const assetCategoryId = searchParams.get("assetCategoryId");
  if (!assetCategoryId) {
    return new Response(null, { status: 400 });
  }

  const apiBaseUrl = process.env.API_INTERNAL_URL ?? "http://localhost:5080";
  const response = await fetch(`${apiBaseUrl}/api/v1/import-batches/template?assetCategoryId=${assetCategoryId}`, {
    headers: { Authorization: `Bearer ${session.accessToken}` },
    cache: "no-store",
  });

  if (!response.ok || !response.body) {
    return new Response(null, { status: response.status });
  }

  return new Response(response.body, {
    status: 200,
    headers: {
      "Content-Type": response.headers.get("Content-Type") ?? "text/csv",
      "Content-Disposition": response.headers.get("Content-Disposition") ?? "attachment",
    },
  });
}
