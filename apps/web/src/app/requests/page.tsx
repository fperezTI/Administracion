import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getInternalRequests, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { formatDate, resolveTimeZone } from "@/lib/format-date";
import { INTERNAL_REQUEST_STATUS_LABELS, INTERNAL_REQUEST_TYPE_LABELS, internalRequestStatusBadgeVariant } from "@/lib/request-labels";

export default async function InternalRequestsPage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
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
    const requests = await getInternalRequests(accessToken, { companyId, pageSize: 100 });

    content = (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Activo</TableHead>
              <TableHead>Tipo</TableHead>
              <TableHead className="hidden sm:table-cell">Solicitante</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead className="hidden sm:table-cell">Fecha</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {requests.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-muted-foreground py-8 text-center">
                  No hay solicitudes todavía.
                </TableCell>
              </TableRow>
            ) : (
              requests.items.map((r) => (
                <TableRow key={r.id}>
                  <TableCell>
                    <Link href={`/requests/${r.id}`} className="hover:text-primary font-mono font-medium hover:underline">
                      {r.assetFolio}
                    </Link>
                  </TableCell>
                  <TableCell>{INTERNAL_REQUEST_TYPE_LABELS[r.type]}</TableCell>
                  <TableCell className="hidden sm:table-cell">{r.requestedByDisplayName}</TableCell>
                  <TableCell>
                    <Badge variant={internalRequestStatusBadgeVariant(r.status)}>{INTERNAL_REQUEST_STATUS_LABELS[r.status]}</Badge>
                  </TableCell>
                  <TableCell className="hidden sm:table-cell">{formatDate(r.requestedAtUtc, timeZone)}</TableCell>
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
        {status === 403 ? "No tienes permiso para consultar solicitudes (Requests.Read)." : "No fue posible consultar las solicitudes."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Solicitudes internas"
        subtitle="Solicitudes de asignación, préstamo o mantenimiento hechas por cualquier persona de la empresa."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href={`/requests/new?companyId=${companyId}`} />}>
          <Plus data-icon="inline-start" />
          Nueva solicitud
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
