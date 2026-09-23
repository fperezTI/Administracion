import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getSpareParts, getMe, type SparePartSortField } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHeader, TableRow } from "@/components/ui/table";
import { SPARE_PART_STATUS_LABELS, sparePartStatusBadgeVariant } from "@/lib/maintenance-labels";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 50;
const DEFAULT_SORT: SparePartSortField = "name";

export default async function SparePartsPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
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
  const sortBy = (params.sortBy as SparePartSortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortDescending === "true";

  let content: React.ReactNode;
  try {
    const spareParts = await getSpareParts(accessToken, { companyId, pageNumber, pageSize: PAGE_SIZE, sortBy, sortDescending });
    const totalPages = Math.max(1, Math.ceil(spareParts.totalCount / PAGE_SIZE));

    content = (
      <>
        <Table>
          <TableHeader>
            <TableRow>
              {[
                { key: "name", label: "Nombre" },
                { key: "serialNumber", label: "Número de serie" },
                { key: "status", label: "Estado" },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/spare-parts"
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
            {spareParts.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={3} className="text-muted-foreground py-8 text-center">
                  No hay refacciones registradas todavía.
                </TableCell>
              </TableRow>
            ) : (
              spareParts.items.map((p) => (
                <TableRow key={p.id}>
                  <TableCell>
                    <Link href={`/spare-parts/${p.id}?companyId=${companyId}`} className="hover:text-primary font-medium hover:underline">
                      {p.name}
                    </Link>
                    {p.partNumber && <span className="text-muted-foreground ml-1 text-xs">({p.partNumber})</span>}
                  </TableCell>
                  <TableCell className="font-mono">{p.serialNumber}</TableCell>
                  <TableCell>
                    <Badge variant={sparePartStatusBadgeVariant(p.status)}>{SPARE_PART_STATUS_LABELS[p.status]}</Badge>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <TablePagination
          basePath="/spare-parts"
          params={params}
          companyId={companyId}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={spareParts.totalCount}
          itemLabel="refacción"
          itemLabelPlural="refacciones"
        />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar refacciones (SpareParts.Read)." : "No fue posible consultar las refacciones."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Refacciones"
        subtitle="Refacciones serializadas — historial de instalación y retiro por activo."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href={`/spare-parts/new?companyId=${companyId}`} />}>
          <Plus data-icon="inline-start" />
          Nueva refacción
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
