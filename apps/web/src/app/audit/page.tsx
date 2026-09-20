import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAuditEntries } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

/** No CompanySwitcher — Audit.Read is a tenant-wide admin permission with no per-company membership
 * filter, same treatment as /companies (see the F8 plan and AppDbContext's remarks on AuditEntry). */
export default async function AuditPage({
  searchParams,
}: {
  searchParams: Promise<{ commandName?: string; fromUtc?: string; toUtc?: string }>;
}) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;

  let content: React.ReactNode;
  try {
    const entries = await getAuditEntries(accessToken, {
      pageSize: 100,
      commandName: params.commandName,
      fromUtc: params.fromUtc ? new Date(params.fromUtc).toISOString() : undefined,
      toUtc: params.toUtc ? new Date(params.toUtc).toISOString() : undefined,
    });

    content = (
      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Fecha</TableHead>
              <TableHead>Usuario</TableHead>
              <TableHead>Comando</TableHead>
              <TableHead>Módulo.Acción</TableHead>
              <TableHead>Resultado</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {entries.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-muted-foreground py-8 text-center">
                  No hay entradas de auditoría todavía.
                </TableCell>
              </TableRow>
            ) : (
              entries.items.map((e) => (
                <TableRow key={e.id}>
                  <TableCell className="text-xs whitespace-nowrap">{new Date(e.occurredAtUtc).toLocaleString("es-MX")}</TableCell>
                  <TableCell>{e.userDisplayName ?? "—"}</TableCell>
                  <TableCell className="font-mono text-xs">{e.commandName}</TableCell>
                  <TableCell className="text-xs">{e.module && e.action ? `${e.module}.${e.action}` : "—"}</TableCell>
                  <TableCell>
                    <Badge variant={e.succeeded ? "success" : "destructive"}>{e.succeeded ? "Éxito" : "Error"}</Badge>
                    {!e.succeeded && e.errorMessage && <p className="text-destructive mt-1 text-xs">{e.errorMessage}</p>}
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
        {status === 403 ? "No tienes permiso para consultar la auditoría (Audit.Read)." : "No fue posible consultar la auditoría."}
      </p>
    );
  }

  return (
    <div className="mx-auto max-w-5xl p-8">
      <AppHeader title="Auditoría" subtitle="Registro de acciones sensibles ejecutadas en el sistema, éxito o fracaso." />
      <form method="GET" className="mb-4 flex flex-wrap items-end gap-3">
        <div className="flex flex-col gap-1">
          <Label htmlFor="commandName">Comando</Label>
          <Input id="commandName" name="commandName" defaultValue={params.commandName} placeholder="CreateAssetCommand" className="h-8 w-56" />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="fromUtc">Desde</Label>
          <Input id="fromUtc" name="fromUtc" type="date" defaultValue={params.fromUtc} className="h-8" />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="toUtc">Hasta</Label>
          <Input id="toUtc" name="toUtc" type="date" defaultValue={params.toUtc} className="h-8" />
        </div>
        <button type="submit" className="border-input bg-secondary text-secondary-foreground h-8 rounded-md border px-3 text-sm">
          Filtrar
        </button>
      </form>
      {content}
    </div>
  );
}
