import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMaintenanceOrders, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { formatDate, resolveTimeZone } from "@/lib/format-date";
import {
  MAINTENANCE_ORDER_STATUS_LABELS,
  MAINTENANCE_ORDER_TYPE_LABELS,
  maintenanceOrderStatusBadgeVariant,
} from "@/lib/maintenance-labels";

export default async function MaintenanceOrdersPage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
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
    const orders = await getMaintenanceOrders(accessToken, { companyId, pageSize: 100 });

    content = (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Folio</TableHead>
              <TableHead>Activo</TableHead>
              <TableHead className="hidden sm:table-cell">Tipo</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead className="hidden sm:table-cell">Abierta</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {orders.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-muted-foreground py-8 text-center">
                  No hay órdenes de mantenimiento todavía.
                </TableCell>
              </TableRow>
            ) : (
              orders.items.map((o) => (
                <TableRow key={o.id}>
                  <TableCell>
                    <Link href={`/maintenance-orders/${o.id}`} className="hover:text-primary font-mono font-medium hover:underline">
                      {o.folio}
                    </Link>
                  </TableCell>
                  <TableCell>{o.assetFolio}</TableCell>
                  <TableCell className="hidden sm:table-cell">{MAINTENANCE_ORDER_TYPE_LABELS[o.type]}</TableCell>
                  <TableCell>
                    <Badge variant={maintenanceOrderStatusBadgeVariant(o.status)}>{MAINTENANCE_ORDER_STATUS_LABELS[o.status]}</Badge>
                  </TableCell>
                  <TableCell className="hidden sm:table-cell">{formatDate(o.openedAtUtc, timeZone)}</TableCell>
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
        {status === 403 ? "No tienes permiso para consultar mantenimientos (Maintenance.Read)." : "No fue posible consultar las órdenes de mantenimiento."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Mantenimiento"
        subtitle="Órdenes de mantenimiento preventivo y correctivo."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href={`/maintenance-orders/new?companyId=${companyId}`} />}>
          <Plus data-icon="inline-start" />
          Nueva orden
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
