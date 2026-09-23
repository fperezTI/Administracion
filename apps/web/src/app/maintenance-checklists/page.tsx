import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMaintenanceChecklists, type MaintenanceChecklistSortField } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHeader, TableRow } from "@/components/ui/table";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 50;
const DEFAULT_SORT: MaintenanceChecklistSortField = "name";

export default async function MaintenanceChecklistsPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const pageNumber = params.pageNumber ? Math.max(1, Number(params.pageNumber)) : 1;
  const sortBy = (params.sortBy as MaintenanceChecklistSortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortDescending === "true";

  let content: React.ReactNode;
  try {
    const checklists = await getMaintenanceChecklists(accessToken, { pageNumber, pageSize: PAGE_SIZE, sortBy, sortDescending });
    const totalPages = Math.max(1, Math.ceil(checklists.totalCount / PAGE_SIZE));

    content = (
      <>
        <Table>
          <TableHeader>
            <TableRow>
              {[
                { key: "name", label: "Nombre" },
                { key: "key", label: "Clave" },
                { key: "latestVersionNumber", label: "Versión actual" },
                { key: "isActive", label: "Estado" },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/maintenance-checklists"
                  params={params}
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
            {checklists.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="text-muted-foreground py-8 text-center">
                  No hay checklists todavía.
                </TableCell>
              </TableRow>
            ) : (
              checklists.items.map((c) => (
                <TableRow key={c.id}>
                  <TableCell>
                    <Link href={`/maintenance-checklists/${c.id}`} className="hover:text-primary font-medium hover:underline">
                      {c.name}
                    </Link>
                  </TableCell>
                  <TableCell className="font-mono">{c.key}</TableCell>
                  <TableCell>v{c.latestVersionNumber}</TableCell>
                  <TableCell>
                    <Badge variant={c.isActive ? "success" : "outline"}>{c.isActive ? "Activo" : "Inactivo"}</Badge>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <TablePagination
          basePath="/maintenance-checklists"
          params={params}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={checklists.totalCount}
          itemLabel="checklist"
          itemLabelPlural="checklists"
        />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar checklists (Maintenance.Read)." : "No fue posible consultar los checklists."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Checklists de mantenimiento"
        subtitle="Listas versionadas de verificación, reutilizables al abrir órdenes de mantenimiento."
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href="/maintenance-checklists/new" />}>
          <Plus data-icon="inline-start" />
          Nuevo checklist
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
