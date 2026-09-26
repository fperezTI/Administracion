import { NextResponse } from "next/server";
import { auth } from "@/auth";

const PROTECTED_PREFIXES = [
  "/dashboard",
  "/assets",
  "/asset-categories",
  "/assignments",
  "/my-assignments",
  "/loans",
  "/movements",
  "/transfers",
  "/approval-flows",
  "/approvals",
  "/my-approvals",
  "/templates",
  "/maintenance-orders",
  "/maintenance-checklists",
  "/warranties",
  "/spare-parts",
  "/consumables",
  "/requests",
  "/my-requests",
  "/imports",
  "/reports",
  "/search",
  "/notifications",
  "/audit",
  "/companies",
  "/org-units",
  "/roles",
  "/users",
];

function isProtectedPath(pathname: string) {
  return PROTECTED_PREFIXES.some((prefix) => pathname === prefix || pathname.startsWith(`${prefix}/`));
}

export default auth((req) => {
  const { pathname } = req.nextUrl;

  // A failed silent token refresh (see src/auth.ts) is treated the same as no session — see
  // src/lib/require-session.ts for why.
  if (isProtectedPath(pathname) && (!req.auth || req.auth.error === "RefreshAccessTokenError")) {
    // req.nextUrl.origin can't be trusted here: NextAuth's auth() wrapper rewrites it to AUTH_URL
    // instead of the request's real host, which produces a redirect to the wrong origin (blocked
    // by the CSP connect-src directive on background Link prefetches). The Host/X-Forwarded-Host
    // headers reflect the actual incoming request, unaffected by that rewrite.
    const host = req.headers.get("x-forwarded-host") ?? req.headers.get("host");
    const protocol = req.headers.get("x-forwarded-proto") ?? req.nextUrl.protocol.replace(":", "");
    const origin = host ? `${protocol}://${host}` : req.nextUrl.origin;
    return NextResponse.redirect(new URL("/", origin));
  }
});

export const config = {
  matcher: [
    "/dashboard/:path*",
    "/assets/:path*",
    "/asset-categories/:path*",
    "/assignments/:path*",
    "/my-assignments/:path*",
    "/loans/:path*",
    "/movements/:path*",
    "/transfers/:path*",
    "/approval-flows/:path*",
    "/approvals/:path*",
    "/my-approvals/:path*",
    "/templates/:path*",
    "/maintenance-orders/:path*",
    "/maintenance-checklists/:path*",
    "/warranties/:path*",
    "/spare-parts/:path*",
    "/consumables/:path*",
    "/requests/:path*",
    "/my-requests/:path*",
    "/imports/:path*",
    "/reports/:path*",
    "/search/:path*",
    "/notifications/:path*",
    "/audit/:path*",
    "/companies/:path*",
    "/org-units/:path*",
    "/roles/:path*",
    "/users/:path*",
  ],
};
