import { NextResponse } from "next/server";
import { auth } from "@/auth";

export default auth((req) => {
  // A failed silent token refresh (see src/auth.ts) is treated the same as no session — see
  // src/lib/require-session.ts for why.
  if (!req.auth || req.auth.error === "RefreshAccessTokenError") {
    return NextResponse.redirect(new URL("/", req.nextUrl.origin));
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
