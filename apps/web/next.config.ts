import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin("./src/i18n/request.ts");

// F12 hardening — same baseline as the API's SecurityHeadersMiddleware, plus a CSP (the API is a pure
// JSON endpoint and doesn't need one; this app renders HTML, so it does). next/font/google self-hosts
// font files at build time (see app/layout.tsx), so no external font host needs to be allowlisted here.
// 'unsafe-inline' on script/style is Next.js's own hydration bootstrap and Tailwind/shadcn inline
// styles — a stricter nonce-based CSP is a real future improvement, documented as out of scope for V1 in
// docs/security/hardening.md.
const securityHeaders = [
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "X-Frame-Options", value: "DENY" },
  { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
  {
    key: "Content-Security-Policy",
    value: [
      "default-src 'self'",
      "script-src 'self' 'unsafe-inline'",
      "style-src 'self' 'unsafe-inline'",
      "img-src 'self' data:",
      "font-src 'self'",
      "connect-src 'self'",
      "frame-ancestors 'none'",
      "base-uri 'self'",
      "form-action 'self'",
      "object-src 'none'",
    ].join("; "),
  },
];

const nextConfig: NextConfig = {
  // Standalone output keeps the production Docker image small (infrastructure/docker/web.Dockerfile)
  // by tracing only the files each route actually needs instead of shipping full node_modules.
  output: "standalone",
  async headers() {
    return [{ source: "/:path*", headers: securityHeaders }];
  },
};

export default withNextIntl(nextConfig);
