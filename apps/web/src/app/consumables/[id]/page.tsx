import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { getConsumables, getConsumableStockMovements, getMe, getOrgUnitTree } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { formatDate, resolveTimeZone } from "@/lib/format-date";
import { CONSUMABLE_STOCK_DIRECTION_LABELS, CONSUMABLE_STOCK_MOVEMENT_REASON_LABELS } from "@/lib/maintenance-labels";
import { RegisterMovementForm } from "./register-movement-form";

export default async function ConsumableDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ companyId?: string }>;
}) {
  const accessToken = await requireAccessToken();
  const { id } = await params;
  const searchParamsValue = await searchParams;
  const me = await getMe(accessToken);

  if (me.companies.length === 0) {
    return <EmptyCompanyState />;
  }

  const companyId = searchParamsValue.companyId && me.companies.some((c) => c.companyId === searchParamsValue.companyId)
    ? searchParamsValue.companyId
    : me.companies[0].companyId;
  const timeZone = resolveTimeZone(me.companies, companyId);

  const [consumables, movements, orgUnits] = await Promise.all([
    getConsumables(accessToken, companyId, { pageSize: 200 }),
    getConsumableStockMovements(accessToken, id),
    getOrgUnitTree(accessToken, companyId),
  ]);
  const consumable = consumables.items.find((c) => c.id === id);
  if (!consumable) {
    notFound();
  }
  const warehouses = orgUnits.filter((o) => o.orgUnitTypeName === "Almacén");
  const belowMinimum = consumable.minimumStock !== null && consumable.currentStock < consumable.minimumStock;

  return (
    <>
      <AppHeader title="Consumibles" />
    <div className="mx-auto flex max-w-3xl flex-col gap-4 px-8 pb-8">
      <div className="mb-2 flex justify-end">
        <Button variant="outline" render={<Link href={`/consumables?companyId=${companyId}`} />}>
          <ArrowLeft data-icon="inline-start" />
          Volver
        </Button>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold tracking-tight">{consumable.name}</h2>
          <p className="text-muted-foreground font-mono text-sm">{consumable.sku ?? "Sin SKU"}</p>
        </div>
        <div className="text-right">
          <p className="text-lg font-semibold tracking-tight">
            {consumable.currentStock} {consumable.unitOfMeasure}
          </p>
          {belowMinimum && <Badge variant="destructive">Bajo mínimo ({consumable.minimumStock})</Badge>}
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">Registrar movimiento</CardTitle>
        </CardHeader>
        <CardContent>
          {warehouses.length === 0 ? (
            <p className="text-muted-foreground text-sm">
              No hay unidades de tipo Almacén en la estructura organizacional.
            </p>
          ) : (
            <RegisterMovementForm consumableId={consumable.id} warehouses={warehouses} />
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">Historial de movimientos</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Folio</TableHead>
                <TableHead>Dirección</TableHead>
                <TableHead>Motivo</TableHead>
                <TableHead>Cantidad</TableHead>
                <TableHead>Fecha</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {movements.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} className="text-muted-foreground py-8 text-center">
                    Sin movimientos todavía.
                  </TableCell>
                </TableRow>
              ) : (
                movements.map((m) => (
                  <TableRow key={m.id}>
                    <TableCell className="font-mono">{m.folio}</TableCell>
                    <TableCell>{CONSUMABLE_STOCK_DIRECTION_LABELS[m.direction]}</TableCell>
                    <TableCell>{CONSUMABLE_STOCK_MOVEMENT_REASON_LABELS[m.reason]}</TableCell>
                    <TableCell>{m.quantity}</TableCell>
                    <TableCell>{formatDate(m.occurredAtUtc, timeZone)}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
    </>
  );
}
