import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getApprovalFlows } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { APPROVAL_MODE_LABELS } from "@/lib/approval-labels";
import { toggleApprovalFlowActiveAction } from "./actions";

export default async function ApprovalFlowsPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const flows = await getApprovalFlows(accessToken);

    content = (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Clave</TableHead>
              <TableHead>Alcance</TableHead>
              <TableHead>Roles aprobadores</TableHead>
              <TableHead>Modo</TableHead>
              <TableHead>Requeridas</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead />
            </TableRow>
          </TableHeader>
          <TableBody>
            {flows.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-muted-foreground py-8 text-center">
                  No hay flujos de aprobación configurados todavía.
                </TableCell>
              </TableRow>
            ) : (
              flows.map((flow) => (
                <TableRow key={flow.id}>
                  <TableCell className="font-mono">{flow.key}</TableCell>
                  <TableCell>{flow.companyId ? "Esta empresa" : "Todas las empresas"}</TableCell>
                  <TableCell>{flow.approverRoles.map((r) => r.roleName).join(flow.mode === "Sequential" ? " → " : ", ")}</TableCell>
                  <TableCell>{APPROVAL_MODE_LABELS[flow.mode]}</TableCell>
                  <TableCell>{flow.requiredApprovals}</TableCell>
                  <TableCell>
                    <Badge variant={flow.isActive ? "success" : "outline"}>{flow.isActive ? "Activo" : "Inactivo"}</Badge>
                  </TableCell>
                  <TableCell>
                    <form action={toggleApprovalFlowActiveAction.bind(null, flow.id, !flow.isActive)}>
                      <Button variant="outline" size="sm" type="submit">
                        {flow.isActive ? "Desactivar" : "Activar"}
                      </Button>
                    </form>
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
        {status === 403 ? "No tienes permiso para consultar flujos de aprobación (Approvals.Read)." : "No fue posible consultar los flujos de aprobación."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Flujos de aprobación"
        subtitle="Qué roles deben aprobar cada tipo de operación (p. ej. baja de activos)."
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href="/approval-flows/new" />}>
          <Plus data-icon="inline-start" />
          Nuevo flujo
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
