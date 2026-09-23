import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getRoles } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export default async function RolesPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const roles = await getRoles(accessToken, { pageSize: 100 });
    content = (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Nombre</TableHead>
              <TableHead>Descripción</TableHead>
              <TableHead>Permisos</TableHead>
              <TableHead>Estado</TableHead>
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
