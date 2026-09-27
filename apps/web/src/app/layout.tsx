import type { Metadata, Viewport } from "next";
import { IBM_Plex_Sans, IBM_Plex_Mono } from "next/font/google";
import { NextIntlClientProvider } from "next-intl";
import { getMessages } from "next-intl/server";
import "./globals.css";
import { ServiceWorkerRegistration } from "@/components/service-worker-registration";
import { AppShell } from "@/components/layout/app-shell";
import { Topbar } from "@/components/layout/topbar";
import { getSession } from "@/auth";
import { resolveThemeCode } from "@/lib/theme-catalog";

const plexSans = IBM_Plex_Sans({
  variable: "--font-plex-sans",
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
});

const plexMono = IBM_Plex_Mono({
  variable: "--font-plex-mono",
  subsets: ["latin"],
  weight: ["400", "500", "600"],
});

export const metadata: Metadata = {
  title: "AssetHub — Gestión de Activos de TI e Infraestructura",
  description: "Administración operativa de activos de TI e infraestructura multiempresa.",
};

export const viewport: Viewport = {
  themeColor: "#113456",
};

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const messages = await getMessages();
  // Resolved server-side (personal preference, else the active company's default, else a safe
  // fallback — see GetMeQuery on the backend) and cached in the session JWT by src/auth.ts, so this
  // never needs its own backend call: no flash, no localStorage, no client-side effect required to
  // apply the initial theme. See lib/theme-catalog.ts for the five valid codes and app/globals.css
  // for what each one actually looks like.
  const session = await getSession();
  const theme = resolveThemeCode(session?.effectiveTheme);

  return (
    <html lang="es" className={theme} suppressHydrationWarning>
      <body
        className={`${plexSans.variable} ${plexMono.variable} antialiased lg:has-[[data-slot="app-sidebar"][data-collapsed="false"]]:pl-56 lg:has-[[data-slot="app-sidebar"][data-collapsed="true"]]:pl-16`}
      >
        <NextIntlClientProvider messages={messages}>
          {/* AppShell decides on the client (usePathname, no Dynamic API) whether to show the
           * Sidebar/Topbar — a root layout that reads headers()/cookies() makes every route fully
           * dynamic and breaks Next.js's same-page "refetch" fast path (pagination/sorting via
           * search params on the same route), which is why that decision doesn't live here. */}
          <AppShell topbar={<Topbar />}>{children}</AppShell>
          <ServiceWorkerRegistration />
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
