import Link from "next/link";
import { signOut } from "@/auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

const NAV_LINKS = [
  { href: "/assets", label: "Activos" },
  { href: "/asset-categories", label: "Categorías" },
  { href: "/assignments", label: "Asignaciones" },
  { href: "/loans", label: "Préstamos" },
  { href: "/movements", label: "Movimientos" },
  { href: "/transfers", label: "Transferencias" },
  { href: "/maintenance-orders", label: "Mantenimiento" },
  { href: "/maintenance-checklists", label: "Checklists" },
  { href: "/warranties", label: "Garantías" },
  { href: "/spare-parts", label: "Refacciones" },
  { href: "/consumables", label: "Consumibles" },
  { href: "/requests", label: "Solicitudes" },
  { href: "/imports", label: "Importaciones" },
  { href: "/reports", label: "Reportes" },
  { href: "/approval-flows", label: "Flujos de aprobación" },
  { href: "/templates", label: "Plantillas" },
  { href: "/companies", label: "Empresas" },
  { href: "/org-units", label: "Estructura" },
  { href: "/roles", label: "Roles" },
  { href: "/users", label: "Usuarios" },
  { href: "/audit", label: "Auditoría" },
];

/** Every authenticated user, regardless of RBAC permissions — these are self-service, not
 * administrative actions (signing your own assignment receipt, deciding on an approval where you hold
 * an eligible role), so they're never gated by a permission the way the rest of NAV_LINKS is. */
const SELF_SERVICE_LINKS = [
  { href: "/my-assignments", label: "Mis asignaciones" },
  { href: "/my-requests", label: "Mis solicitudes" },
  { href: "/my-approvals", label: "Mis aprobaciones" },
  { href: "/notifications", label: "Mis notificaciones" },
];

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
    <div className="mb-6 flex flex-col gap-3 border-b pb-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <span
            aria-hidden
            className="bg-primary text-primary-foreground flex size-8 shrink-0 items-center justify-center rounded-md font-mono text-xs font-semibold tracking-tight"
          >
            AT
          </span>
          <div>
            <h1 className="text-lg leading-tight font-semibold tracking-tight">{title}</h1>
            {subtitle && <p className="text-muted-foreground text-sm">{subtitle}</p>}
          </div>
        </div>
        <div className="flex items-center gap-3">
          <form method="GET" action="/search" className="flex items-center">
            <Input name="term" placeholder="Buscar…" className="h-8 w-40" />
          </form>
          {activeCompany}
          <form action={federatedSignOut}>
            <Button variant="outline" type="submit">
              Cerrar sesión
            </Button>
          </form>
        </div>
      </div>
      <nav className="flex flex-wrap gap-x-5 gap-y-1 text-sm">
        {NAV_LINKS.map((link) => (
          <Link
            key={link.href}
            href={link.href}
            className="hover:text-primary decoration-primary underline-offset-4 hover:underline"
          >
            {link.label}
          </Link>
        ))}
        <span className="ml-auto flex flex-wrap gap-x-5 gap-y-1">
          {SELF_SERVICE_LINKS.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className="text-primary decoration-primary underline-offset-4 hover:underline"
            >
              {link.label}
            </Link>
          ))}
        </span>
      </nav>
    </div>
  );
}
