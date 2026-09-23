import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMe, getMovements, type MovementSortField, type MovementType } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Table, TableBody, TableCell, TableHeader, TableRow } from "@/components/ui/table";
import { formatDateTime, resolveTimeZone } from "@/lib/format-date";
import { MOVEMENT_TYPE_LABELS } from "@/lib/inventory-labels";
import { MovementFilterForm } from "./movement-filter-form";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 30;
const DEFAULT_SORT: MovementSortField = "effectiveAtUtc";

export default async function MovementsPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);

  if (me.companies.length === 0) {
    return <EmptyCompanyState />;
  }

  const companyId = params.companyId && me.companies.some((c) => c.companyId === params.companyId)
    ? params.companyId
    : me.companies[0].companyId;
  const timeZone = resolveTimeZone(me.companies, companyId);
  const pageNumber = params.pageNumber ? Math.max(1, Number(params.pageNumber)) : 1;
  // Sin sortBy en la URL, el default histórico del backend es effectiveAtUtc descendente (más
  // reciente primero) sin importar sortDescending — reflejarlo aquí para que la flecha del
  // encabezado "Fecha" aparezca activa y apuntando hacia abajo desde la primera carga.
  const sortBy = (params.sortBy as MovementSortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortBy === undefined ? true : params.sortDescending === "true";

  let content: React.ReactNode;
  try {
    const movements = await getMovements(accessToken, {
      companyId,
      pageNumber,
      pageSize: PAGE_SIZE,
      type: params.type as MovementType | undefined,
      sortBy,
      sortDescending,
    });
    const totalPages = Math.max(1, Math.ceil(movements.totalCount / PAGE_SIZE));

    content = (
      <>
          <Table>
            <TableHeader>
              <TableRow>
                {[
                  { key: "folioNumber", label: "Folio", className: undefined },
                  { key: "assetFolio", label: "Activo", className: undefined },
                  { key: "type", label: "Tipo", className: undefined },
                  { key: "effectiveAtUtc", label: "Fecha", className: "hidden sm:table-cell" },
                  { key: "notes", label: "Notas", className: "hidden sm:table-cell" },
                ].map((column) => (
                  <SortableTableHead
                    key={column.key}
                    basePath="/movements"
                    params={params}
                    companyId={companyId}
                    sortKey={column.key}
                    defaultSortKey={DEFAULT_SORT}
                    currentSortBy={params.sortBy}
                    currentSortDescending={sortDescending}
                    className={column.className}
                  >
                    {column.label}
                  </SortableTableHead>
                ))}
              </TableRow>
            </TableHeader>
            <TableBody>
              {movements.items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="text-muted-foreground py-8 text-center">
                    No hay movimientos con estos filtros.
                  </TableCell>
                </TableRow>
              ) : (
                movements.items.map((m) => (
                  <TableRow key={m.id}>
                    <TableCell className="font-mono">{m.folioNumber}</TableCell>
                    <TableCell>
                      <Link href={`/assets/${m.assetId}`} className="hover:text-primary font-mono hover:underline">
                        {m.assetFolio}
                      </Link>
                    </TableCell>
                    <TableCell>{MOVEMENT_TYPE_LABELS[m.type]}</TableCell>
                    <TableCell className="hidden sm:table-cell">{formatDateTime(m.effectiveAtUtc, timeZone)}</TableCell>
                    <TableCell className="hidden text-muted-foreground sm:table-cell">{m.notes ?? "—"}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        <TablePagination
          basePath="/movements"
          params={params}
          companyId={companyId}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={movements.totalCount}
          itemLabel="movimiento"
          itemLabelPlural="movimientos"
        />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar movimientos (Movements.Read)." : "No fue posible consultar los movimientos."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Movimientos"
        subtitle="Historial auditado de asignaciones, préstamos, devoluciones y reubicaciones."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <MovementFilterForm companyId={companyId} defaultType={params.type ?? ""} />
      {content}
    </div>
    </>
  );
}
