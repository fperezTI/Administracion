import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getApprovalFlows, type ApprovalFlowSortField } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { APPROVAL_MODE_LABELS } from "@/lib/approval-labels";
import { toggleApprovalFlowActiveAction } from "./actions";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 50;
const DEFAULT_SORT: ApprovalFlowSortField = "key";

export default async function ApprovalFlowsPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const pageNumber = params.pageNumber ? Math.max(1, Number(params.pageNumber)) : 1;
  const sortBy = (params.sortBy as ApprovalFlowSortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortDescending === "true";

  let content: React.ReactNode;
  try {
    const flows = await getApprovalFlows(accessToken, { pageNumber, pageSize: PAGE_SIZE, sortBy, sortDescending });
    const totalPages = Math.max(1, Math.ceil(flows.totalCount / PAGE_SIZE));

    content = (
      <>
        <Table>
          <TableHeader>
            <TableRow>
              {[
                { key: "key", label: "Clave" },
                { key: "companyId", label: "Alcance" },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/approval-flows"
                  params={params}
                  sortKey={column.key}
                  defaultSortKey={DEFAULT_SORT}
                  currentSortBy={params.sortBy}
                  currentSortDescending={sortDescending}
                >
                  {column.label}
                </SortableTableHead>
              ))}
              {/* No es ordenable: es una lista de roles, no un valor escalar. */}
              <TableHead>Roles aprobadores</TableHead>
              {[
                { key: "mode", label: "Modo" },
                { key: "requiredApprovals", label: "Requeridas" },
                { key: "isActive", label: "Estado" },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/approval-flows"
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
            {flows.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-muted-foreground py-8 text-center">
                  No hay flujos de aprobación configurados todavía.
                </TableCell>
              </TableRow>
            ) : (
              flows.items.map((flow) => (
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
        <TablePagination
          basePath="/approval-flows"
          params={params}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={flows.totalCount}
          itemLabel="flujo"
          itemLabelPlural="flujos"
        />
      </>
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
