import Link from "next/link";

export type SearchParams = Record<string, string | undefined>;

function buildPageHref(basePath: string, params: SearchParams, companyId: string, pageNumber: number) {
  const query = new URLSearchParams({
    ...(Object.fromEntries(Object.entries(params).filter(([, v]) => v !== undefined)) as Record<string, string>),
    companyId,
    pageNumber: String(pageNumber),
  });
  return `${basePath}?${query.toString()}`;
}

function PageLink({
  href,
  disabled,
  children,
}: {
  href: string;
  disabled: boolean;
  children: React.ReactNode;
}) {
  if (disabled) {
    return (
      <span aria-disabled="true" className="text-muted-foreground/50 rounded-lg border px-3 py-1">
        {children}
      </span>
    );
  }

  return (
    <Link href={href} className="rounded-lg border px-3 py-1 hover:bg-muted">
      {children}
    </Link>
  );
}

/** Paginación server-driven compartida por las páginas de listado que exponen "página siguiente"
 * (hoy Activos y Movimientos) — navega vía query string, sin estado de cliente, igual que el
 * filtrado GET del resto de la página. */
export function TablePagination({
  basePath,
  params,
  companyId,
  pageNumber,
  totalPages,
  totalCount,
  itemLabel,
  itemLabelPlural,
}: {
  basePath: string;
  params: SearchParams;
  companyId: string;
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  itemLabel: string;
  itemLabelPlural: string;
}) {
  return (
    <div className="mt-4 flex items-center justify-between text-sm">
      <p className="text-muted-foreground">
        {totalCount} {totalCount === 1 ? itemLabel : itemLabelPlural} — página {pageNumber} de {totalPages}
      </p>
      <div className="flex gap-2">
        <PageLink href={buildPageHref(basePath, params, companyId, pageNumber - 1)} disabled={pageNumber <= 1}>
          Anterior
        </PageLink>
        <PageLink
          href={buildPageHref(basePath, params, companyId, pageNumber + 1)}
          disabled={pageNumber >= totalPages}
        >
          Siguiente
        </PageLink>
      </div>
    </div>
  );
}
