import { AppNav } from "@/components/layout/app-nav";
import { Topbar } from "@/components/layout/topbar";

/** Shell corporativo compartido: Sidebar + Topbar + área de contenido. Se monta una sola vez desde
 * el root layout (ver app/layout.tsx) para las rutas que lo necesitan — la decisión de qué rutas
 * quedan fuera (login público, vistas imprimibles) vive en el layout, no aquí. */
export function AppShell({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen">
      <AppNav />
      <div className="flex min-h-screen flex-col">
        <Topbar />
        <main className="flex-1">{children}</main>
      </div>
    </div>
  );
}
