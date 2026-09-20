"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  ArrowLeftRight,
  ChevronDown,
  ClipboardCheck,
  Database,
  Laptop,
  Menu,
  Settings,
  Wrench,
  type LucideIcon,
} from "lucide-react";
import { cn } from "cn";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { NAV_GROUPS, SELF_SERVICE_LINKS } from "./nav-links";

function isActiveHref(pathname: string, href: string) {
  return pathname === href || pathname.startsWith(`${href}/`);
}

function activeGroupLabel(pathname: string) {
  return NAV_GROUPS.find((group) => group.links.some((link) => isActiveHref(pathname, link.href)))?.label;
}

/** Un ícono por categoría, no por link — ayuda a reconocer la sección de un vistazo sin subir
 * el ruido visual (21 íconos distintos serían más difíciles de escanear que 6). */
const GROUP_ICONS: Record<string, LucideIcon> = {
  Activos: Laptop,
  "Inventario y movimientos": ArrowLeftRight,
  Mantenimiento: Wrench,
  "Solicitudes y aprobaciones": ClipboardCheck,
  Datos: Database,
  Administración: Settings,
};

function NavGroupList({
  pathname,
  onNavigate,
}: {
  pathname: string;
  onNavigate?: () => void;
}) {
  const currentGroup = activeGroupLabel(pathname);
  const [openGroup, setOpenGroup] = useState<string | undefined>(currentGroup);

  // Al navegar a un link de otra categoría, esa categoría se abre sola — igual que un menú de
  // sistema operativo: la sección donde estás nunca queda escondida. Acordeón: una sola abierta
  // a la vez, así que esto también cierra la anterior.
  useEffect(() => {
    if (currentGroup) {
      setOpenGroup(currentGroup);
    }
  }, [currentGroup]);

  function toggleGroup(label: string) {
    setOpenGroup((prev) => (prev === label ? undefined : label));
  }

  return (
    <>
      <div className="flex flex-col gap-0.5 border-b pb-3">
        {SELF_SERVICE_LINKS.map((link) => {
          const active = isActiveHref(pathname, link.href);
          return (
            <Link
              key={link.href}
              href={link.href}
              onClick={onNavigate}
              aria-current={active ? "page" : undefined}
              className={cn(
                "text-primary rounded-md px-2 py-1.5 text-sm",
                active ? "bg-accent font-semibold" : "hover:bg-muted"
              )}
            >
              {link.label}
            </Link>
          );
        })}
      </div>
      {NAV_GROUPS.map((group) => {
        const Icon = GROUP_ICONS[group.label];
        const isOpen = openGroup === group.label;
        return (
          <div key={group.label}>
            <button
              type="button"
              onClick={() => toggleGroup(group.label)}
              aria-expanded={isOpen}
              className="hover:bg-muted flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm font-semibold"
            >
              {Icon && <Icon className="size-4 shrink-0" aria-hidden />}
              <span className="flex-1">{group.label}</span>
              <ChevronDown
                className={cn("text-muted-foreground size-4 shrink-0 transition-transform", isOpen && "rotate-180")}
                aria-hidden
              />
            </button>
            {isOpen && (
              <div className="mt-0.5 flex flex-col gap-0.5 pl-6">
                {group.links.map((link) => {
                  const active = isActiveHref(pathname, link.href);
                  return (
                    <Link
                      key={link.href}
                      href={link.href}
                      onClick={onNavigate}
                      aria-current={active ? "page" : undefined}
                      className={cn(
                        "rounded-md px-2 py-1.5 text-sm",
                        active ? "bg-accent text-primary font-medium" : "hover:bg-muted"
                      )}
                    >
                      {link.label}
                    </Link>
                  );
                })}
              </div>
            )}
          </div>
        );
      })}
    </>
  );
}

export function AppNav() {
  const pathname = usePathname();
  const [open, setOpen] = useState(false);

  return (
    <>
      {/* <1024px: botón de hamburguesa que abre el drawer con la misma nav agrupada. */}
      <div className="lg:hidden">
        <Button variant="outline" size="icon" aria-label="Abrir menú" onClick={() => setOpen(true)}>
          <Menu className="size-4" />
        </Button>
      </div>

      <Sheet open={open} onOpenChange={setOpen}>
        <SheetContent side="left" className="overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Navegación</SheetTitle>
          </SheetHeader>
          <nav className="flex flex-col gap-1 px-4 pb-4">
            <NavGroupList pathname={pathname} onNavigate={() => setOpen(false)} />
          </nav>
        </SheetContent>
      </Sheet>

      {/* >=1024px: sidebar fijo. Saca su propio espacio vía body:has(...) en layout.tsx — ninguna
       * página necesita saber que existe. */}
      <aside
        data-slot="app-sidebar"
        className="fixed top-0 left-0 z-40 hidden h-screen w-56 flex-col border-r bg-card p-4 lg:flex"
      >
        <Link href="/dashboard" className="mb-4 flex items-center gap-2" aria-label="Ir a mi dashboard">
          <span
            aria-hidden
            className="bg-primary text-primary-foreground flex size-8 shrink-0 items-center justify-center rounded-md font-mono text-xs font-semibold tracking-tight"
          >
            AT
          </span>
        </Link>
        <nav className="flex flex-1 flex-col gap-1 overflow-y-auto">
          <NavGroupList pathname={pathname} />
        </nav>
      </aside>
    </>
  );
}
