import type { Metadata, Viewport } from "next";
import { headers } from "next/headers";
import { IBM_Plex_Sans, IBM_Plex_Mono } from "next/font/google";
import { NextIntlClientProvider } from "next-intl";
import { getMessages } from "next-intl/server";
import "./globals.css";
import { ThemeProvider } from "@/components/theme-provider";
import { ServiceWorkerRegistration } from "@/components/service-worker-registration";
import { AppShell } from "@/components/layout/app-shell";

// Rutas que existen fuera del App Shell: el login público ("/") y las dos vistas pensadas para
// imprimirse a pantalla completa (protegidas por su cuenta vía requireAccessToken(), pero sin
// sidebar/topbar). El pathname llega vía el header que src/middleware.ts anota en cada request —
// es la forma documentada de leer la ruta actual desde un layout de servidor en el App Router.
const CHROMELESS_ROUTE_PATTERNS = [/^\/assets\/[^/]+\/label$/, /^\/assignments\/[^/]+\/resguardo$/];

function isChromelessRoute(pathname: string) {
  return pathname === "/" || CHROMELESS_ROUTE_PATTERNS.some((pattern) => pattern.test(pathname));
}

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
  const headersList = await headers();
  const pathname = headersList.get("x-pathname") ?? "";
  const showShell = !isChromelessRoute(pathname);

  return (
    <html lang="es" suppressHydrationWarning>
      <body
        className={`${plexSans.variable} ${plexMono.variable} antialiased lg:has-[[data-slot="app-sidebar"][data-collapsed="false"]]:pl-56 lg:has-[[data-slot="app-sidebar"][data-collapsed="true"]]:pl-16`}
      >
        <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
          <NextIntlClientProvider messages={messages}>
            {showShell ? <AppShell>{children}</AppShell> : <main>{children}</main>}
            <ServiceWorkerRegistration />
          </NextIntlClientProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
