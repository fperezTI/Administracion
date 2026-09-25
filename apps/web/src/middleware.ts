import { NextResponse } from "next/server";
import { auth } from "@/auth";

// Misma lista de rutas protegidas que antes (sin cambios de comportamiento de seguridad) — el
// matcher de abajo ahora es más amplio porque también necesitamos correr en TODAS las rutas
// (incluida "/") para poder anotar el pathname actual vía un header y que el root layout
// (app/layout.tsx) decida ahí si monta el App Shell o no. Ver AppShell/isChromelessRoute.
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
    // by the CSP connect-src directive on background Link prefetches — see AppNav's self-service
    // links, which every page now renders). The Host/X-Forwarded-Host headers are the actual
    // incoming request, unaffected by that rewrite.
    const host = req.headers.get("x-forwarded-host") ?? req.headers.get("host");
    const protocol = req.headers.get("x-forwarded-proto") ?? req.nextUrl.protocol.replace(":", "");
    const origin = host ? `${protocol}://${host}` : req.nextUrl.origin;
    return NextResponse.redirect(new URL("/", origin));
  }

  const requestHeaders = new Headers(req.headers);
  requestHeaders.set("x-pathname", pathname);
  return NextResponse.next({ request: { headers: requestHeaders } });
});

export const config = {
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico|sw.js|icons|manifest.webmanifest).*)"],
};
