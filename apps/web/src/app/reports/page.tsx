import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import {
  ApiError,
  getExpiringWarranties,
  getInventorySummary,
  getLowStockConsumables,
  getMaintenanceKpis,
  getMe,
  getReportExportUrl,
} from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { StatTile } from "@/components/ui/stat-tile";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { ASSET_STATUS_LABELS } from "@/lib/asset-labels";
import { WARRANTY_TYPE_LABELS } from "@/lib/maintenance-labels";

function formatNumber(value: number | null, suffix: string): string {
  return value === null ? "—" : `${value.toLocaleString("es-MX", { maximumFractionDigits: 1 })} ${suffix}`;
}

/** Barra de magnitud de un solo tono — no son series distintas a diferenciar, son la misma
 * medida (conteo) repartida por categoría/estado, así que un solo hue basta (ver skill de
 * dataviz: "compare magnitude" → color secuencial, no categórico). */
function MagnitudeBars({ items }: { items: { label: string; count: number }[] }) {
  const max = Math.max(1, ...items.map((i) => i.count));
  return (
    <ul className="flex flex-col gap-2">
      {items.map((item) => (
        <li key={item.label} className="flex items-center gap-2 text-sm">
          <span className="w-32 shrink-0 truncate">{item.label}</span>
          <span className="bg-primary/15 h-2 flex-1 rounded-full">
            <span
              className="bg-primary block h-2 rounded-full"
              style={{ width: `${Math.round((item.count / max) * 100)}%` }}
            />
          </span>
          <span className="w-8 shrink-0 text-right font-medium tabular-nums">{item.count}</span>
        </li>
      ))}
    </ul>
  );
}

export default async function ReportsPage({
  searchParams,
}: {
  searchParams: Promise<{ companyId?: string; consolidated?: string; withinDays?: string }>;
}) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);

  if (me.companies.length === 0) {
    return <EmptyCompanyState />;
  }

  const consolidated = params.consolidated === "1";
  const companyId = consolidated
    ? undefined
    : params.companyId && me.companies.some((c) => c.companyId === params.companyId)
      ? params.companyId
      : me.companies[0].companyId;
  const withinDays = params.withinDays ? Math.max(0, Number(params.withinDays)) : 30;

  let content: React.ReactNode;
  try {
    const [inventory, kpis, warranties, lowStock] = await Promise.all([
      getInventorySummary(accessToken, companyId),
      getMaintenanceKpis(accessToken, companyId),
      getExpiringWarranties(accessToken, companyId, withinDays),
      getLowStockConsumables(accessToken, companyId),
    ]);

    const overdueWarranties = warranties.filter((w) => w.daysRemaining < 0).length;
    const warrantiesTone = warranties.length === 0 ? "neutral" : overdueWarranties > 0 ? "destructive" : "warning";
    const lowStockTone = lowStock.length === 0 ? "neutral" : "destructive";

    content = (
      <div className="flex flex-col gap-4">
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
          <StatTile label="Activos" value={inventory.totalAssets.toLocaleString("es-MX")} />
          <StatTile label="MTTR" value={formatNumber(kpis.mttrHours, "h")} />
          <StatTile label="MTBF" value={formatNumber(kpis.mtbfDays, "d")} />
          <StatTile label="Órdenes cerradas" value={kpis.closedOrdersCount.toLocaleString("es-MX")} />
          <StatTile label="Garantías por vencer" value={warranties.length} tone={warrantiesTone} />
          <StatTile label="Existencias bajas" value={lowStock.length} tone={lowStockTone} />
        </div>

        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Inventario</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-6 sm:grid-cols-2">
            <div>
              <p className="text-muted-foreground mb-2 text-xs font-medium tracking-wide uppercase">Por estado</p>
              <MagnitudeBars items={inventory.byStatus.map((s) => ({ label: ASSET_STATUS_LABELS[s.status], count: s.count }))} />
            </div>
            <div>
              <p className="text-muted-foreground mb-2 text-xs font-medium tracking-wide uppercase">Por categoría</p>
              <MagnitudeBars
                items={inventory.byCategory.map((c) => ({ label: c.assetCategoryName, count: c.count }))}
              />
            </div>
          </CardContent>
        </Card>

        {kpis.byCategory.length > 0 && (
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-sm">Mantenimiento por categoría</CardTitle>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" render={<a href={getReportExportUrl("maintenance-kpis", "Xlsx", companyId)} />}>
                  Exportar Excel
                </Button>
                <Button variant="outline" size="sm" render={<a href={getReportExportUrl("maintenance-kpis", "Pdf", companyId)} />}>
                  Exportar PDF
                </Button>
              </div>
            </CardHeader>
            <CardContent className="p-0">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Categoría</TableHead>
                    <TableHead>MTTR</TableHead>
                    <TableHead>MTBF</TableHead>
                    <TableHead>Órdenes cerradas</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {kpis.byCategory.map((c) => (
                    <TableRow key={c.assetCategoryId}>
                      <TableCell>{c.assetCategoryName}</TableCell>
                      <TableCell>{formatNumber(c.mttrHours, "h")}</TableCell>
                      <TableCell>{formatNumber(c.mtbfDays, "días")}</TableCell>
                      <TableCell>{c.closedOrdersCount}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        )}

        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="text-sm">Garantías por vencer</CardTitle>
            <div className="flex items-center gap-2">
              <form method="GET" className="flex items-end gap-2">
                {companyId && <input type="hidden" name="companyId" value={companyId} />}
                {consolidated && <input type="hidden" name="consolidated" value="1" />}
                <div className="flex flex-col gap-1">
                  <Label htmlFor="withinDays" className="text-xs">Días</Label>
                  <Input id="withinDays" name="withinDays" type="number" min={0} defaultValue={withinDays} className="h-8 w-20" />
                </div>
                <Button type="submit" size="sm" variant="outline">Filtrar</Button>
              </form>
              <Button variant="outline" size="sm" render={<a href={getReportExportUrl("expiring-warranties", "Xlsx", companyId, withinDays)} />}>
                Excel
              </Button>
              <Button variant="outline" size="sm" render={<a href={getReportExportUrl("expiring-warranties", "Pdf", companyId, withinDays)} />}>
                PDF
              </Button>
            </div>
          </CardHeader>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Folio</TableHead>
                  <TableHead>Proveedor</TableHead>
                  <TableHead>Tipo</TableHead>
                  <TableHead>Vence</TableHead>
                  <TableHead>Días restantes</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {warranties.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="text-muted-foreground py-8 text-center">
                      Sin garantías por vencer en esta ventana.
                    </TableCell>
                  </TableRow>
                ) : (
                  warranties.map((w) => (
                    <TableRow key={w.warrantyId}>
                      <TableCell>
                        <Link href={`/assets/${w.assetId}`} className="hover:text-primary font-mono hover:underline">
                          {w.assetFolio}
                        </Link>
                      </TableCell>
                      <TableCell>{w.provider}</TableCell>
                      <TableCell>{WARRANTY_TYPE_LABELS[w.type]}</TableCell>
                      <TableCell>{w.endDate}</TableCell>
                      <TableCell>
                        <Badge variant={w.daysRemaining < 0 ? "destructive" : "warning"}>
                          {w.daysRemaining < 0 ? `Vencida hace ${-w.daysRemaining} días` : `${w.daysRemaining} días`}
                        </Badge>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle className="text-sm">Existencias bajas</CardTitle>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" render={<a href={getReportExportUrl("low-stock-consumables", "Xlsx", companyId)} />}>
                Excel
              </Button>
              <Button variant="outline" size="sm" render={<a href={getReportExportUrl("low-stock-consumables", "Pdf", companyId)} />}>
                PDF
              </Button>
            </div>
          </CardHeader>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nombre</TableHead>
                  <TableHead>SKU</TableHead>
                  <TableHead>Existencia actual</TableHead>
                  <TableHead>Existencia mínima</TableHead>
                  <TableHead>Faltante</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {lowStock.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} className="text-muted-foreground py-8 text-center">
                      Sin consumibles bajo el mínimo.
                    </TableCell>
                  </TableRow>
                ) : (
                  lowStock.map((c) => (
                    <TableRow key={c.consumableId}>
                      <TableCell>{c.name}</TableCell>
                      <TableCell>{c.sku ?? "—"}</TableCell>
                      <TableCell>
                        {c.currentStock} {c.unitOfMeasure}
                      </TableCell>
                      <TableCell>
                        {c.minimumStock} {c.unitOfMeasure}
                      </TableCell>
                      <TableCell>
                        <Badge variant="destructive">{c.shortfall}</Badge>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </div>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403
          ? consolidated
            ? "No tienes permiso para consultar reportes consolidados (Reports.ReadConsolidated)."
            : "No tienes permiso para consultar reportes (Reports.Read)."
          : "No fue posible consultar los reportes."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Reportes"
        subtitle="Paneles operativos: inventario, mantenimiento, garantías y existencias."
        activeCompany={companyId ? <CompanySwitcher companies={me.companies} currentCompanyId={companyId} /> : undefined}
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button
          variant="outline"
          size="sm"
          render={<Link href={consolidated ? `/reports?companyId=${me.companies[0].companyId}` : "/reports?consolidated=1"} />}
        >
          {consolidated ? "Ver por empresa" : "Ver consolidado (todas las empresas)"}
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
