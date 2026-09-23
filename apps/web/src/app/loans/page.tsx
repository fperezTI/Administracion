import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getLoans, getMe, type LoanSortField } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHeader, TableRow } from "@/components/ui/table";
import { LOAN_STATUS_LABELS, loanStatusBadgeVariant } from "@/lib/inventory-labels";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 50;
const DEFAULT_SORT: LoanSortField = "loanedAtUtc";

export default async function LoansPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
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
  const sortBy = (params.sortBy as LoanSortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortBy === undefined ? true : params.sortDescending === "true";

  let content: React.ReactNode;
  try {
    const loans = await getLoans(accessToken, { companyId, pageNumber, pageSize: PAGE_SIZE, sortBy, sortDescending });
    const totalPages = Math.max(1, Math.ceil(loans.totalCount / PAGE_SIZE));

    content = (
      <>
        <Table>
          <TableHeader>
            <TableRow>
              {[
                { key: "assetFolio", label: "Folio", className: undefined },
                { key: "borrowerDisplayName", label: "Prestado a", className: undefined },
                { key: "expectedReturnDate", label: "Devolución esperada", className: "hidden sm:table-cell" },
                { key: "status", label: "Estado", className: undefined },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/loans"
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
            {loans.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="text-muted-foreground py-8 text-center">
                  No hay préstamos todavía.
                </TableCell>
              </TableRow>
            ) : (
              loans.items.map((loan) => (
                <TableRow key={loan.id}>
                  <TableCell>
                    <Link href={`/loans/${loan.id}`} className="hover:text-primary font-mono font-medium hover:underline">
                      {loan.assetFolio}
                    </Link>
                  </TableCell>
                  <TableCell>{loan.borrowerDisplayName}</TableCell>
                  <TableCell className="hidden sm:table-cell">{loan.expectedReturnDate}</TableCell>
                  <TableCell>
                    <Badge variant={loanStatusBadgeVariant(loan.status)}>{LOAN_STATUS_LABELS[loan.status]}</Badge>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <TablePagination
          basePath="/loans"
          params={params}
          companyId={companyId}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={loans.totalCount}
          itemLabel="préstamo"
          itemLabelPlural="préstamos"
        />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar préstamos (Loans.Read)." : "No fue posible consultar los préstamos."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Préstamos"
        subtitle="Préstamos de corto plazo, sin firma de recepción."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href={`/loans/new?companyId=${companyId}`} />}>
          <Plus data-icon="inline-start" />
          Nuevo préstamo
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
