import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getImportBatches, getMe, type ImportBatchSortField } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHeader, TableRow } from "@/components/ui/table";
import { formatDateTime, resolveTimeZone } from "@/lib/format-date";
import { IMPORT_BATCH_STATUS_LABELS, importBatchStatusBadgeVariant } from "@/lib/import-export-labels";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 50;
const DEFAULT_SORT: ImportBatchSortField = "createdAtUtc";

export default async function ImportBatchesPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
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
  // Sin sortBy en la URL, el default histórico del backend es createdAtUtc descendente (más
  // reciente primero) sin importar sortDescending.
  const sortBy = (params.sortBy as ImportBatchSortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortBy === undefined ? true : params.sortDescending === "true";

  let content: React.ReactNode;
  try {
    const batches = await getImportBatches(accessToken, {
      companyId,
      pageNumber,
      pageSize: PAGE_SIZE,
      sortBy,
      sortDescending,
    });
    const totalPages = Math.max(1, Math.ceil(batches.totalCount / PAGE_SIZE));

    content = (
      <>
        <Table>
          <TableHeader>
            <TableRow>
              {[
                { key: "fileName", label: "Archivo" },
                { key: "status", label: "Estado" },
                { key: "totalRows", label: "Filas" },
                { key: "createdAtUtc", label: "Subido" },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/imports"
                  params={params}
                  companyId={companyId}
                  sortKey={column.key}
                  defaultSortKey={DEFAULT_SORT}
                  currentSortBy={params.sortBy}
                  currentSortDescending={sortDescending}
                >
                  {column.label}
                </SortableTableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {batches.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="text-muted-foreground py-8 text-center">
                  No hay lotes de importación todavía.
                </TableCell>
              </TableRow>
            ) : (
              batches.items.map((b) => (
                <TableRow key={b.id}>
                  <TableCell>
                    <Link href={`/imports/${b.id}`} className="hover:text-primary font-medium hover:underline">
                      {b.fileName}
                    </Link>
                  </TableCell>
                  <TableCell>
                    <Badge variant={importBatchStatusBadgeVariant(b.status)}>{IMPORT_BATCH_STATUS_LABELS[b.status]}</Badge>
                  </TableCell>
                  <TableCell className="text-muted-foreground text-sm">
                    {b.totalRows === null
                      ? "—"
                      : `${b.validRows ?? 0} válidas / ${b.invalidRows ?? 0} inválidas de ${b.totalRows}`}
                  </TableCell>
                  <TableCell>{formatDateTime(b.createdAtUtc, timeZone)}</TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <TablePagination
          basePath="/imports"
          params={params}
          companyId={companyId}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={batches.totalCount}
          itemLabel="lote"
          itemLabelPlural="lotes"
        />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar importaciones (Imports.Read)." : "No fue posible consultar los lotes de importación."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Importaciones"
        subtitle="Carga masiva de activos por archivo CSV."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href={`/imports/new?companyId=${companyId}`} />}>
          <Plus data-icon="inline-start" />
          Nueva importación
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
