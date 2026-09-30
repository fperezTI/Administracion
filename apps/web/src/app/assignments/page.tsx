import Link from "next/link";
import { FileSpreadsheet, FileText, Plus, X } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import {
  ApiError,
  getAssignments,
  getAssignmentsExportUrl,
  getMe,
  getOrgUnitTree,
  type AssignmentSortField,
  type AssignmentStatus,
} from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { formatDate, resolveTimeZone } from "@/lib/format-date";
import { ASSIGNMENT_STATUS_LABELS, assignmentStatusBadgeVariant } from "@/lib/inventory-labels";
import { flattenOrgUnitTree } from "@/lib/org-unit-tree";
import { cancelAssignmentAction } from "./[id]/actions";
import { AssignmentFilterForm } from "./assignment-filter-form";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 50;
const DEFAULT_SORT: AssignmentSortField = "assignedAtUtc";

export default async function AssignmentsPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);

  if (me.companies.length === 0) {
    return <EmptyCompanyState />;
  }

  const companyId = params.companyId && me.companies.some((c) => c.companyId === params.companyId)
    ? params.companyId
    : me.companies[0].companyId;
  const timeZone = resolveTimeZone(me.companies, companyId);
  const pageNumber = params.pageNumber ? Math.max(1, Number(params.pageNumber)) : 1;
  const sortBy = (params.sortBy as AssignmentSortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortBy === undefined ? true : params.sortDescending === "true";

  const filters = {
    status: params.status as AssignmentStatus | undefined,
    assignedToSearch: params.assignedToSearch,
    search: params.search,
    assignedFrom: params.assignedFrom,
    assignedTo: params.assignedTo,
    orgUnitId: params.orgUnitId,
  };

  let content: React.ReactNode;
  try {
    const [assignments, orgUnitNodes] = await Promise.all([
      getAssignments(accessToken, {
        companyId,
        pageNumber,
        pageSize: PAGE_SIZE,
        sortBy,
        sortDescending,
        ...filters,
      }),
      // Filtro "opcional" — un usuario con Assignments.Read pero sin Structure.Read (permiso distinto)
      // debe poder seguir viendo/filtrando asignaciones, solo sin el desplegable de Área.
      getOrgUnitTree(accessToken, companyId).catch(() => []),
    ]);
    const orgUnits = flattenOrgUnitTree(orgUnitNodes);
    const totalPages = Math.max(1, Math.ceil(assignments.totalCount / PAGE_SIZE));

    content = (
      <>
        <AssignmentFilterForm
          companyId={companyId}
          orgUnits={orgUnits}
          defaultStatus={params.status ?? ""}
          defaultAssignedToSearch={params.assignedToSearch ?? ""}
          defaultSearch={params.search ?? ""}
          defaultAssignedFrom={params.assignedFrom ?? ""}
          defaultAssignedTo={params.assignedTo ?? ""}
          defaultOrgUnitId={params.orgUnitId ?? ""}
        />

        <Table containerClassName="max-h-[calc(100vh-23rem)] overflow-y-auto">
          <TableHeader>
            <TableRow>
              {[
                { key: "assetFolio", label: "Folio", className: undefined },
                { key: "assignedToDisplayName", label: "Asignado a", className: undefined },
                { key: "status", label: "Estado", className: undefined },
                { key: "assignedAtUtc", label: "Fecha", className: "hidden sm:table-cell" },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/assignments"
                  params={params}
                  companyId={companyId}
                  sortKey={column.key}
                  defaultSortKey={DEFAULT_SORT}
                  currentSortBy={params.sortBy}
                  currentSortDescending={sortDescending}
                  className={column.className}
                >
                  {column.label}
                </SortableTableHead>
              ))}
              <TableHead className="text-right">Acciones</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {assignments.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-muted-foreground py-8 text-center">
                  No hay asignaciones con estos filtros.
                </TableCell>
              </TableRow>
            ) : (
              assignments.items.map((a) => (
                <TableRow key={a.id}>
                  <TableCell>
                    <Link href={`/assignments/${a.id}`} className="hover:text-primary font-mono font-medium hover:underline">
                      {a.assetFolio}
                    </Link>
                    {a.groupId && (
                      <Badge variant="secondary" className="ml-2">
                        Paquete
                      </Badge>
                    )}
                  </TableCell>
                  <TableCell>{a.assignedToDisplayName}</TableCell>
                  <TableCell>
                    <Badge variant={assignmentStatusBadgeVariant(a.status)}>{ASSIGNMENT_STATUS_LABELS[a.status]}</Badge>
                  </TableCell>
                  <TableCell className="hidden sm:table-cell">{formatDate(a.assignedAtUtc, timeZone)}</TableCell>
                  <TableCell className="text-right">
                    {a.status === "PendingSignature" && (
                      <form action={cancelAssignmentAction.bind(null, a.id)}>
                        <Button type="submit" variant="outline" size="sm">
                          <X data-icon="inline-start" />
                          Cancelar
                        </Button>
                      </form>
                    )}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <TablePagination
          basePath="/assignments"
          params={params}
          companyId={companyId}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={assignments.totalCount}
          itemLabel="asignación"
          itemLabelPlural="asignaciones"
        />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar asignaciones (Assignments.Read)." : "No fue posible consultar las asignaciones."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Asignaciones"
        subtitle="Activos entregados en resguardo a una persona."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex items-center justify-end gap-3">
        <Button variant="outline" render={<a href={getAssignmentsExportUrl({ companyId, format: "Xlsx", ...filters })} />}>
          <FileSpreadsheet data-icon="inline-start" />
          Exportar Excel
        </Button>
        <Button variant="outline" render={<a href={getAssignmentsExportUrl({ companyId, format: "Pdf", ...filters })} />}>
          <FileText data-icon="inline-start" />
          Exportar PDF
        </Button>
        <Button render={<Link href={`/assignments/new?companyId=${companyId}`} />}>
          <Plus data-icon="inline-start" />
          Nueva asignación
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
