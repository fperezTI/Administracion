import { signOut } from "@/auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ThemeToggle } from "@/components/theme-toggle";
import { AppNav } from "@/components/layout/app-nav";
import { Logomark } from "@/components/logomark";

/** Clears the local session and also ends the session at Entra ID (federated logout) — without this,
 * "cerrar sesión" only forgets the app's cookie; the browser would still be signed in to Microsoft. */
async function federatedSignOut() {
  "use server";
  await signOut({ redirect: false });
  const baseUrl = process.env.AUTH_URL ?? "http://localhost:3000";
  const tenantId = process.env.ENTRA_TENANT_ID;
  const { redirect } = await import("next/navigation");
  redirect(
    `https://login.microsoftonline.com/${tenantId}/oauth2/v2.0/logout?post_logout_redirect_uri=${encodeURIComponent(baseUrl)}`,
  );
}

export function AppHeader({
  title,
  subtitle,
  activeCompany,
}: {
  title: string;
  subtitle?: string;
  /** Selector de empresa activa u otro control que deba quedar junto al título — nunca escondido
   * más abajo en la página: en un sistema multiempresa, equivocar la empresa activa es un riesgo
   * funcional, no solo un detalle visual. */
  activeCompany?: React.ReactNode;
}) {
  return (
    <header className="mb-6 flex flex-col gap-3 border-b px-8 pt-8 pb-3">
      {/* Ya no vive dentro del contenedor "mx-auto max-w-*" de cada página (Fase de consistencia
       * de header): siempre ocupa todo el ancho disponible junto al sidebar, sin importar que el
       * contenido de abajo use una columna angosta (formularios) o ancha (listas). */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <Logomark className="lg:hidden" />
          <div>
            <h1 className="text-lg leading-tight font-semibold tracking-tight">{title}</h1>
            {subtitle && <p className="text-muted-foreground text-sm">{subtitle}</p>}
          </div>
        </div>
        <div className="flex flex-wrap items-center justify-end gap-3">
          <form method="GET" action="/search" className="flex items-center">
            <Input name="term" placeholder="Buscar…" className="h-8 w-40 sm:w-120" />
          </form>
          {activeCompany}
          <ThemeToggle />
          <form action={federatedSignOut}>
            <Button variant="outline" type="submit">
              Cerrar sesión
            </Button>
          </form>
        </div>
      </div>
      <AppNav />
    </header>
  );
}
