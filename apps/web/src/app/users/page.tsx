import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMe, getUsers, type UserSortField } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHeader, TableRow } from "@/components/ui/table";
import { formatDateTime, resolveTimeZone } from "@/lib/format-date";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 50;
const DEFAULT_SORT: UserSortField = "displayName";

export default async function UsersPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);
  // Users are inherently multi-company and this list has no CompanySwitcher (tenant-wide admin view) —
  // falls back to the viewing admin's own first company.
  const timeZone = resolveTimeZone(me.companies, null);

  const pageNumber = params.pageNumber ? Math.max(1, Number(params.pageNumber)) : 1;
  const sortBy = (params.sortBy as UserSortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortDescending === "true";

  let content: React.ReactNode;
  try {
    const users = await getUsers(accessToken, { pageNumber, pageSize: PAGE_SIZE, sortBy, sortDescending });
    const totalPages = Math.max(1, Math.ceil(users.totalCount / PAGE_SIZE));
    content = (
      <>
        <Table>
          <TableHeader>
            <TableRow>
              {[
                { key: "displayName", label: "Nombre" },
                { key: "email", label: "Correo" },
                { key: "isActive", label: "Estado" },
                { key: "lastLoginAtUtc", label: "Último acceso" },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/users"
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
            {users.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="text-muted-foreground py-8 text-center">
                  Todavía no hay usuarios (se crean automáticamente en el primer inicio de sesión).
                </TableCell>
              </TableRow>
            ) : (
              users.items.map((user) => (
                <TableRow key={user.id}>
                  <TableCell className="font-medium">
                    <Link href={`/users/${user.id}`} className="hover:underline">
                      {user.displayName}
                    </Link>
                  </TableCell>
                  <TableCell>{user.email}</TableCell>
                  <TableCell>
                    <Badge variant={user.isActive ? "success" : "outline"}>{user.isActive ? "Activo" : "Inactivo"}</Badge>
                  </TableCell>
                  <TableCell>
                    {user.lastLoginAtUtc ? formatDateTime(user.lastLoginAtUtc, timeZone) : "—"}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <TablePagination
          basePath="/users"
          params={params}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={users.totalCount}
          itemLabel="usuario"
          itemLabelPlural="usuarios"
        />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar usuarios (Users.Read)." : "No fue posible consultar los usuarios."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Usuarios"
        subtitle="Los perfiles se crean automáticamente en el primer inicio de sesión con Entra ID, o puedes agregarlos antes desde el directorio."
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href="/users/new" />}>
          <Plus data-icon="inline-start" />
          Agregar desde el directorio
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
