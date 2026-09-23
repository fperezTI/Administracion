import Link from "next/link";
import { Plus, X } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssignments, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { formatDate, resolveTimeZone } from "@/lib/format-date";
import { ASSIGNMENT_STATUS_LABELS, assignmentStatusBadgeVariant } from "@/lib/inventory-labels";
import { cancelAssignmentAction } from "./[id]/actions";

export default async function AssignmentsPage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
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

  let content: React.ReactNode;
  try {
    const assignments = await getAssignments(accessToken, { companyId, pageSize: 100 });

    content = (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Folio</TableHead>
              <TableHead>Asignado a</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead className="hidden sm:table-cell">Fecha</TableHead>
              <TableHead className="text-right">Acciones</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {assignments.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-muted-foreground py-8 text-center">
                  No hay asignaciones todavía.
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
      <div className="mb-4 flex justify-end">
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
