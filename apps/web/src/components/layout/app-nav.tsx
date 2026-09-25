"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";
import {
  ArrowLeftRight,
  Bell,
  CheckCircle2,
  ChevronDown,
  ChevronsLeft,
  ChevronsRight,
  ClipboardCheck,
  ClipboardList,
  Database,
  FileText,
  Laptop,
  Menu,
  Settings,
  Wrench,
  type LucideIcon,
} from "lucide-react";
import { cn } from "cn";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { Logomark } from "@/components/logomark";
import { NAV_GROUPS, SELF_SERVICE_LINKS } from "./nav-links";

function isActiveHref(pathname: string, href: string) {
  return pathname === href || pathname.startsWith(`${href}/`);
}

function activeGroupLabel(pathname: string) {
  return NAV_GROUPS.find((group) => group.links.some((link) => isActiveHref(pathname, link.href)))?.label;
}

/** Un ícono por categoría, no por link — ayuda a reconocer la sección de un vistazo sin subir
 * el ruido visual (21 íconos distintos serían más difíciles de escanear que 6). También es lo
 * único que queda visible en el riel colapsado (ver CollapsedNavRail). */
const GROUP_ICONS: Record<string, LucideIcon> = {
  Activos: Laptop,
  "Inventario y movimientos": ArrowLeftRight,
  Mantenimiento: Wrench,
  "Solicitudes y aprobaciones": ClipboardCheck,
  Datos: Database,
  Administración: Settings,
};

/** SELF_SERVICE_LINKS no tiene ícono en su forma expandida (son 4 links de texto), pero el riel
 * colapsado necesita algo que mostrar — set acotado, a diferencia de darle ícono a los ~21 links
 * de NAV_GROUPS, que obligaría a curar un ícono por cada uno. */
const SELF_SERVICE_ICONS: Record<string, LucideIcon> = {
  "/my-assignments": ClipboardList,
  "/my-requests": FileText,
  "/my-approvals": CheckCircle2,
  "/notifications": Bell,
};

function useGroupAccordion(pathname: string) {
  const currentGroup = activeGroupLabel(pathname);
  const [openGroup, setOpenGroupState] = useState<string | undefined>(currentGroup);

  // Al navegar a un link de otra categoría, esa categoría se abre sola — igual que un menú de
  // sistema operativo: la sección donde estás nunca queda escondida. Acordeón: una sola abierta
  // a la vez, así que esto también cierra la anterior.
  useEffect(() => {
    if (currentGroup) {
      setOpenGroupState(currentGroup);
    }
  }, [currentGroup]);

  function toggleGroup(label: string) {
    setOpenGroupState((prev) => (prev === label ? undefined : label));
  }

  return { openGroup, toggleGroup, setOpenGroup: setOpenGroupState };
}

function NavGroupList({
  pathname,
  openGroup,
  onToggleGroup,
  onNavigate,
}: {
  pathname: string;
  openGroup: string | undefined;
  onToggleGroup: (label: string) => void;
  onNavigate?: () => void;
}) {
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
              onClick={() => onToggleGroup(group.label)}
              aria-expanded={isOpen}
              className="hover:bg-muted flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm font-semibold"
            >
              {Icon && <Icon className="size-4 shrink-0 stroke-[1.5]" aria-hidden />}
              <span className="flex-1">{group.label}</span>
              <ChevronDown
                className={cn(
                  "text-muted-foreground size-4 shrink-0 stroke-[1.5] transition-transform",
                  isOpen && "rotate-180"
                )}
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
                        "relative rounded-md px-2 py-1.5 text-sm",
                        active
                          ? "bg-accent text-primary before:bg-brand font-medium before:absolute before:top-1.5 before:bottom-1.5 before:-left-3 before:w-0.5 before:rounded-full"
                          : "hover:bg-muted"
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

/** Riel colapsado (~64px): solo íconos de grupo + self-service, cada uno con tooltip accesible.
 * Clic en un ícono de grupo expande el sidebar y abre esa categoría — evita necesitar flyouts o
 * un ícono por cada uno de los ~21 links individuales. */
function CollapsedNavRail({
  pathname,
  onExpandGroup,
}: {
  pathname: string;
  onExpandGroup: (label: string) => void;
}) {
  return (
    <TooltipProvider>
      <div className="flex flex-col items-center gap-0.5 border-b pb-3">
        {SELF_SERVICE_LINKS.map((link) => {
          const Icon = SELF_SERVICE_ICONS[link.href];
          const active = isActiveHref(pathname, link.href);
          return (
            <Tooltip key={link.href}>
              <TooltipTrigger
                render={<Link href={link.href} aria-current={active ? "page" : undefined} />}
                aria-label={link.label}
                className={cn(
                  "flex size-9 shrink-0 items-center justify-center rounded-md",
                  active ? "bg-accent text-accent-foreground" : "text-muted-foreground hover:bg-muted hover:text-foreground"
                )}
              >
                {Icon && <Icon className="size-4 stroke-[1.5]" aria-hidden />}
              </TooltipTrigger>
              <TooltipContent side="right">{link.label}</TooltipContent>
            </Tooltip>
          );
        })}
      </div>
      <div className="flex flex-col items-center gap-0.5 pt-3">
        {NAV_GROUPS.map((group) => {
          const Icon = GROUP_ICONS[group.label];
          const active = activeGroupLabel(pathname) === group.label;
          return (
            <Tooltip key={group.label}>
              <TooltipTrigger
                type="button"
                onClick={() => onExpandGroup(group.label)}
                aria-label={group.label}
                className={cn(
                  "flex size-9 shrink-0 items-center justify-center rounded-md",
                  active ? "bg-accent text-accent-foreground" : "text-muted-foreground hover:bg-muted hover:text-foreground"
                )}
              >
                {Icon && <Icon className="size-4 stroke-[1.5]" aria-hidden />}
              </TooltipTrigger>
              <TooltipContent side="right">{group.label}</TooltipContent>
            </Tooltip>
          );
        })}
      </div>
    </TooltipProvider>
  );
}

/** Botón de menú móvil + drawer (<1024px) — vive junto al resto del chrome global en Topbar, no
 * en el aside de escritorio (que está `hidden` en ese breakpoint). Comparte NavGroupList con
 * AppNav pero mantiene su propio estado de acordeón: son dos superficies independientes. */
export function MobileNav() {
  const pathname = usePathname();
  const t = useTranslations("Layout");
  const [open, setOpen] = useState(false);
  const { openGroup, toggleGroup, setOpenGroup } = useGroupAccordion(pathname);

  return (
    <>
      <Button variant="outline" size="icon" aria-label={t("openMenu")} onClick={() => setOpen(true)}>
        <Menu className="size-4 stroke-[1.5]" />
      </Button>
      <Sheet
        open={open}
        onOpenChange={(nextOpen) => {
          setOpen(nextOpen);
          if (nextOpen) {
            setOpenGroup(activeGroupLabel(pathname));
          }
        }}
      >
        <SheetContent side="left" className="overflow-y-auto">
          <SheetHeader>
            <SheetTitle>{t("navigationTitle")}</SheetTitle>
          </SheetHeader>
          <nav aria-label={t("mainNavigation")} className="flex flex-col gap-1 px-4 pb-4">
            <NavGroupList
              pathname={pathname}
              openGroup={openGroup}
              onToggleGroup={toggleGroup}
              onNavigate={() => setOpen(false)}
            />
          </nav>
        </SheetContent>
      </Sheet>
    </>
  );
}

/** Sidebar fijo de escritorio (>=1024px). Saca su propio espacio vía body:has(...) en
 * layout.tsx — ninguna página necesita saber que existe, incluido el ancho cuando está
 * colapsado (ver el selector para data-collapsed). Estado de colapso es efímero (no persiste
 * entre sesiones ni recargas): no hay un patrón existente en el proyecto para persistir
 * preferencias de UI, y no corresponde inventar uno aquí. */
export function AppNav() {
  const pathname = usePathname();
  const t = useTranslations("Layout");
  const [collapsed, setCollapsed] = useState(false);
  const { openGroup, toggleGroup, setOpenGroup } = useGroupAccordion(pathname);

  function expandAndOpenGroup(label: string) {
    setCollapsed(false);
    setOpenGroup(label);
  }

  return (
    <aside
      data-slot="app-sidebar"
      data-collapsed={collapsed}
      className={cn(
        "fixed top-0 left-0 z-40 hidden h-screen flex-col border-r bg-card lg:flex",
        collapsed ? "w-16 items-center p-2" : "w-56 p-4"
      )}
    >
      <div className={cn("mb-4 flex w-full items-center", collapsed ? "flex-col gap-2" : "justify-between gap-2")}>
        <Link href="/dashboard" aria-label={t("goToDashboard")} className="flex items-center">
          <Logomark withWordmark={!collapsed} />
        </Link>
        <Button
          variant="ghost"
          size="icon-sm"
          aria-label={collapsed ? t("expandSidebar") : t("collapseSidebar")}
          onClick={() => setCollapsed((prev) => !prev)}
        >
          {collapsed ? (
            <ChevronsRight className="size-4 stroke-[1.5]" aria-hidden />
          ) : (
            <ChevronsLeft className="size-4 stroke-[1.5]" aria-hidden />
          )}
        </Button>
      </div>

      {collapsed ? (
        <nav aria-label={t("mainNavigation")} className="flex flex-1 flex-col overflow-y-auto">
          <CollapsedNavRail pathname={pathname} onExpandGroup={expandAndOpenGroup} />
        </nav>
      ) : (
        <nav aria-label={t("mainNavigation")} className="flex flex-1 flex-col gap-1 overflow-y-auto">
          <NavGroupList pathname={pathname} openGroup={openGroup} onToggleGroup={toggleGroup} />
        </nav>
      )}
    </aside>
  );
}
