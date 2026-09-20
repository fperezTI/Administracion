import { auth } from "@/auth";

const ALLOWED_REPORTS = new Set(["expiring-warranties", "low-stock-consumables", "maintenance-kpis"]);

/**
 * Proxies a report export from the .NET API — same reasoning as app/api/exports/assets/route.ts (F9):
 * the API is only reachable server-side, and a plain `<a href>` can't attach an Authorization header. One
 * generic proxy for all three report exports (`report` query param picks the backend endpoint) instead of
 * three near-identical route files.
 */
export async function GET(request: Request) {
  const session = await auth();
  if (!session?.accessToken || session.error === "RefreshAccessTokenError") {
    return new Response(null, { status: 401 });
  }

  const { searchParams } = new URL(request.url);
  const report = searchParams.get("report");
  if (!report || !ALLOWED_REPORTS.has(report)) {
    return new Response(null, { status: 400 });
  }

  const forwardedParams = new URLSearchParams(searchParams);
  forwardedParams.delete("report");

  const apiBaseUrl = process.env.API_INTERNAL_URL ?? "http://localhost:5080";
  const response = await fetch(`${apiBaseUrl}/api/v1/reports/${report}/export?${forwardedParams.toString()}`, {
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
