"use client";

import { usePathname } from "next/navigation";
import { AppNav } from "@/components/layout/app-nav";

// Rutas que existen fuera del App Shell: el login público ("/") y las dos vistas pensadas para
// imprimirse a pantalla completa (protegidas por su cuenta vía requireAccessToken(), pero sin
// sidebar/topbar). Esta decisión vive en un Client Component (usePathname, no una Dynamic API de
// servidor como headers()) a propósito: el root layout NO puede depender de headers()/cookies()
// para esto — eso vuelve dinámica toda la ruta y rompe el "refetch" que usa Next.js para navegar
// dentro de la misma página cuando solo cambian los search params (paginación, orden de columnas).
const CHROMELESS_ROUTE_PATTERNS = [/^\/assets\/[^/]+\/label$/, /^\/assignments\/[^/]+\/resguardo$/];

function isChromelessRoute(pathname: string) {
  return pathname === "/" || CHROMELESS_ROUTE_PATTERNS.some((pattern) => pattern.test(pathname));
}

/** Shell corporativo compartido: Sidebar + Topbar + área de contenido. `topbar` llega ya renderizado
 * desde el root layout (Server Component, ahí es donde corre auth()) — este componente solo decide
 * si mostrarlo, no lo renderiza él mismo. */
export function AppShell({ topbar, children }: { topbar: React.ReactNode; children: React.ReactNode }) {
  const pathname = usePathname();

  if (isChromelessRoute(pathname)) {
    return <main>{children}</main>;
  }

  return (
    <div className="min-h-screen">
      <AppNav />
      <div className="flex min-h-screen flex-col">
        {topbar}
        <main className="flex-1">{children}</main>
      </div>
    </div>
  );
}
