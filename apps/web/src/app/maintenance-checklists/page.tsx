import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMaintenanceChecklists } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export default async function MaintenanceChecklistsPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const checklists = await getMaintenanceChecklists(accessToken);

    content = (
      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Nombre</TableHead>
              <TableHead>Clave</TableHead>
              <TableHead>Versión actual</TableHead>
              <TableHead>Estado</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {checklists.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="text-muted-foreground py-8 text-center">
                  No hay checklists todavía.
                </TableCell>
              </TableRow>
            ) : (
              checklists.map((c) => (
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
      </div>
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
    <div className="mx-auto max-w-3xl p-8">
      <AppHeader
        title="Checklists de mantenimiento"
        subtitle="Listas versionadas de verificación, reutilizables al abrir órdenes de mantenimiento."
      />
      <div className="mb-4 flex justify-end">
        <Button render={<Link href="/maintenance-checklists/new" />}>Nuevo checklist</Button>
      </div>
      {content}
    </div>
  );
}
