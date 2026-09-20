import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getUsers } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export default async function UsersPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const users = await getUsers(accessToken, { pageSize: 100 });
    content = (
      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Nombre</TableHead>
              <TableHead>Correo</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead>Último acceso</TableHead>
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
                    {user.lastLoginAtUtc ? new Date(user.lastLoginAtUtc).toLocaleString("es-MX") : "—"}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
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
    <div className="mx-auto max-w-3xl p-8">
      <AppHeader title="Usuarios" subtitle="Los perfiles se crean automáticamente en el primer inicio de sesión con Entra ID." />
      {content}
    </div>
  );
}
