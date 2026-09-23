import Link from "next/link";
import { ArrowDown, ArrowUp, ArrowUpDown } from "lucide-react";
import { TableHead } from "@/components/ui/table";
import { cn } from "cn";
import type { SearchParams } from "./table-pagination";

function buildSortHref(
  basePath: string,
  params: SearchParams,
  companyId: string | undefined,
  sortKey: string,
  nextDescending: boolean
) {
  const query = new URLSearchParams({
    ...(Object.fromEntries(Object.entries(params).filter(([, v]) => v !== undefined)) as Record<string, string>),
    pageNumber: "1",
    sortBy: sortKey,
    // Siempre explícito (nunca se omite): a diferencia de Activos (default ascendente), pantallas
    // como Movimientos/Auditoría por default ordenan descendente — omitir el valor cuando es "false"
    // sería ambiguo con "todavía no se eligió, usa el default de esta columna".
    sortDescending: String(nextDescending),
  });
  if (companyId) {
    query.set("companyId", companyId);
  }
  return `${basePath}?${query.toString()}`;
}

/** Encabezado de tabla ordenable, compartido por los listados server-driven (mismo patrón sin
 * estado de cliente que TablePagination): navega vía query string (sortBy/sortDescending),
 * reinicia a la página 1 al cambiar el orden. `defaultSortKey` es la columna activa cuando la URL
 * todavía no trae `sortBy` (el ORDER BY por default del backend). */
export function SortableTableHead({
  basePath,
  params,
  companyId,
  sortKey,
  defaultSortKey,
  currentSortBy,
  currentSortDescending,
  className,
  children,
}: {
  basePath: string;
  params: SearchParams;
  /** Omitido en pantallas sin selector de empresa (p. ej. Auditoría). */
  companyId?: string;
  sortKey: string;
  defaultSortKey: string;
  currentSortBy: string | undefined;
  currentSortDescending: boolean;
  className?: string;
  children: React.ReactNode;
}) {
  const isActive = (currentSortBy ?? defaultSortKey) === sortKey;
  const nextDescending = isActive ? !currentSortDescending : false;

  return (
    <TableHead className={className}>
      <Link
        href={buildSortHref(basePath, params, companyId, sortKey, nextDescending)}
        className={cn(
          "inline-flex items-center gap-1 hover:text-foreground",
          isActive ? "text-foreground" : "text-muted-foreground"
        )}
      >
        {children}
        {isActive ? (
          currentSortDescending ? (
            <ArrowDown className="size-3.5 stroke-[1.5]" aria-hidden />
          ) : (
            <ArrowUp className="size-3.5 stroke-[1.5]" aria-hidden />
          )
        ) : (
          <ArrowUpDown className="text-muted-foreground/50 size-3.5 stroke-[1.5]" aria-hidden />
        )}
      </Link>
    </TableHead>
  );
}
