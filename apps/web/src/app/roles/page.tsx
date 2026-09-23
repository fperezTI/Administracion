import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getRoles, type RoleSortField } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHeader, TableRow } from "@/components/ui/table";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 50;
const DEFAULT_SORT: RoleSortField = "name";

export default async function RolesPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const pageNumber = params.pageNumber ? Math.max(1, Number(params.pageNumber)) : 1;
  const sortBy = (params.sortBy as RoleSortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortDescending === "true";

  let content: React.ReactNode;
  try {
    const roles = await getRoles(accessToken, { pageNumber, pageSize: PAGE_SIZE, sortBy, sortDescending });
    const totalPages = Math.max(1, Math.ceil(roles.totalCount / PAGE_SIZE));
    content = (
      <>
        <Table>
          <TableHeader>
            <TableRow>
              {[
                { key: "name", label: "Nombre" },
                { key: "description", label: "Descripción" },
                { key: "permissionCount", label: "Permisos" },
                { key: "isActive", label: "Estado" },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/roles"
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
            {roles.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="text-muted-foreground py-8 text-center">
                  Todavía no hay roles.
                </TableCell>
              </TableRow>
            ) : (
              roles.items.map((role) => (
                <TableRow key={role.id}>
                  <TableCell className="font-medium">
                    <Link href={`/roles/${role.id}`} className="hover:underline">
                      {role.name}
                    </Link>
                  </TableCell>
                  <TableCell>{role.description ?? "—"}</TableCell>
                  <TableCell>{role.permissionCount}</TableCell>
                  <TableCell>
                    <Badge variant={role.isActive ? "success" : "outline"}>{role.isActive ? "Activo" : "Inactivo"}</Badge>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <TablePagination
          basePath="/roles"
          params={params}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={roles.totalCount}
          itemLabel="rol"
          itemLabelPlural="roles"
        />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar roles (Roles.Read)." : "No fue posible consultar los roles."}
      </p>
    );
  }

  return (
    <>
      <AppHeader title="Roles" subtitle="Los permisos son globales — la empresa no acota qué puede hacer un rol." />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href="/roles/new" />}>
          <Plus data-icon="inline-start" />
          Nuevo rol
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
