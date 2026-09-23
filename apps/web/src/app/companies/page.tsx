import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getCompanies, type CompanySortField } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { toggleCompanyActiveAction } from "./actions";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 50;
const DEFAULT_SORT: CompanySortField = "tradeName";

export default async function CompaniesPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const pageNumber = params.pageNumber ? Math.max(1, Number(params.pageNumber)) : 1;
  const sortBy = (params.sortBy as CompanySortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortDescending === "true";

  let content: React.ReactNode;
  try {
    const companies = await getCompanies(accessToken, { pageNumber, pageSize: PAGE_SIZE, sortBy, sortDescending });
    const totalPages = Math.max(1, Math.ceil(companies.totalCount / PAGE_SIZE));
    content = (
      <>
        <Table>
          <TableHeader>
            <TableRow>
              {[
                { key: "tradeName", label: "Nombre comercial" },
                { key: "legalName", label: "Razón social" },
                { key: "taxId", label: "RFC / Id. fiscal" },
                { key: "baseCurrency", label: "Moneda" },
                { key: "timeZone", label: "Zona horaria" },
                { key: "isActive", label: "Estado" },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/companies"
                  params={params}
                  sortKey={column.key}
                  defaultSortKey={DEFAULT_SORT}
                  currentSortBy={params.sortBy}
                  currentSortDescending={sortDescending}
                >
                  {column.label}
                </SortableTableHead>
              ))}
              <TableHead />
            </TableRow>
          </TableHeader>
          <TableBody>
            {companies.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-muted-foreground py-8 text-center">
                  Todavía no hay empresas registradas.
                </TableCell>
              </TableRow>
            ) : (
              companies.items.map((company) => (
                <TableRow key={company.id}>
                  <TableCell className="font-medium">{company.tradeName}</TableCell>
                  <TableCell>{company.legalName}</TableCell>
                  <TableCell>{company.taxId}</TableCell>
                  <TableCell>{company.baseCurrency}</TableCell>
                  <TableCell>{company.timeZone}</TableCell>
                  <TableCell>
                    <Badge variant={company.isActive ? "success" : "outline"}>
                      {company.isActive ? "Activa" : "Inactiva"}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <form action={toggleCompanyActiveAction.bind(null, company.id, !company.isActive)}>
                      <Button type="submit" variant="outline" size="sm">
                        {company.isActive ? "Desactivar" : "Activar"}
                      </Button>
                    </form>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <TablePagination
          basePath="/companies"
          params={params}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={companies.totalCount}
          itemLabel="empresa"
          itemLabelPlural="empresas"
        />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar empresas (Companies.Read)." : "No fue posible consultar las empresas."}
      </p>
    );
  }

  return (
    <>
      <AppHeader title="Empresas" subtitle="Hasta 50 empresas dentro de un único tenant de Entra ID." />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href="/companies/new" />}>
          <Plus data-icon="inline-start" />
          Nueva empresa
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
