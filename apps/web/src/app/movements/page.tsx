import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMe, getMovements, type MovementType } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { MOVEMENT_TYPE_LABELS } from "@/lib/inventory-labels";
import { MovementTypeFilterField } from "./movement-type-filter-field";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";

const PAGE_SIZE = 30;

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
  const pageNumber = params.pageNumber ? Math.max(1, Number(params.pageNumber)) : 1;

  let content: React.ReactNode;
  try {
    const movements = await getMovements(accessToken, {
      companyId,
      pageNumber,
      pageSize: PAGE_SIZE,
      type: params.type as MovementType | undefined,
    });
    const totalPages = Math.max(1, Math.ceil(movements.totalCount / PAGE_SIZE));

    content = (
      <>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Folio</TableHead>
                <TableHead>Activo</TableHead>
                <TableHead>Tipo</TableHead>
                <TableHead className="hidden sm:table-cell">Fecha</TableHead>
                <TableHead className="hidden sm:table-cell">Notas</TableHead>
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
                    <TableCell className="hidden sm:table-cell">{new Date(m.effectiveAtUtc).toLocaleString("es-MX")}</TableCell>
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
      <form method="GET" className="mb-4 flex flex-wrap items-end gap-3">
        <input type="hidden" name="companyId" value={companyId} />
        <MovementTypeFilterField defaultType={params.type ?? ""} />
      </form>
      {content}
    </div>
    </>
  );
}
