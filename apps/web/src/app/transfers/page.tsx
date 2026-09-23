import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMe, getTransfers, type TransferSortField } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHeader, TableRow } from "@/components/ui/table";
import { formatDate, resolveTimeZone } from "@/lib/format-date";
import { TRANSFER_STATUS_LABELS, transferStatusBadgeVariant } from "@/lib/transfer-labels";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 50;
const DEFAULT_SORT: TransferSortField = "requestedAtUtc";

export default async function TransfersPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
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
  // Sin sortBy en la URL, el default histórico del backend es requestedAtUtc descendente.
  const sortBy = (params.sortBy as TransferSortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortBy === undefined ? true : params.sortDescending === "true";

  let content: React.ReactNode;
  try {
    const transfers = await getTransfers(accessToken, { companyId, pageNumber, pageSize: PAGE_SIZE, sortBy, sortDescending });
    const totalPages = Math.max(1, Math.ceil(transfers.totalCount / PAGE_SIZE));

    content = (
      <>
        <Table>
          <TableHeader>
            <TableRow>
              {[
                { key: "assetFolio", label: "Folio", className: undefined },
                { key: "fromCompanyName", label: "Origen", className: "hidden sm:table-cell" },
                { key: "toCompanyName", label: "Destino", className: undefined },
                { key: "status", label: "Estado", className: undefined },
                { key: "requestedAtUtc", label: "Fecha", className: "hidden sm:table-cell" },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/transfers"
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
            {transfers.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-muted-foreground py-8 text-center">
                  No hay transferencias todavía.
                </TableCell>
              </TableRow>
            ) : (
              transfers.items.map((t) => (
                <TableRow key={t.id}>
                  <TableCell>
                    <Link href={`/transfers/${t.id}`} className="hover:text-primary font-mono font-medium hover:underline">
                      {t.assetFolio}
                    </Link>
                  </TableCell>
                  <TableCell className="hidden sm:table-cell">{t.fromCompanyName}</TableCell>
                  <TableCell>{t.toCompanyName}</TableCell>
                  <TableCell>
                    <Badge variant={transferStatusBadgeVariant(t.status)}>{TRANSFER_STATUS_LABELS[t.status]}</Badge>
                  </TableCell>
                  <TableCell className="hidden sm:table-cell">
                    {formatDate(t.requestedAtUtc, resolveTimeZone(me.companies, t.fromCompanyId))}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <TablePagination
          basePath="/transfers"
          params={params}
          companyId={companyId}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={transfers.totalCount}
          itemLabel="transferencia"
          itemLabelPlural="transferencias"
        />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar transferencias (Transfers.Read)." : "No fue posible consultar las transferencias."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Transferencias entre empresas"
        subtitle="Activos en tránsito o transferidos entre las empresas del tenant — aparece si tu empresa es origen o destino."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href={`/transfers/new?companyId=${companyId}`} />}>
          <Plus data-icon="inline-start" />
          Nueva transferencia
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
