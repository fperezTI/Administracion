import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssets, getOrgUnitTree, getSparePartById } from "@/lib/api";
import { SPARE_PART_STATUS_LABELS, sparePartStatusBadgeVariant } from "@/lib/maintenance-labels";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { InstallSparePartForm } from "./install-spare-part-form";
import { UninstallSparePartForm } from "./uninstall-spare-part-form";
import { disposeSparePartAction } from "./actions";

export default async function SparePartDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ companyId?: string }>;
}) {
  const accessToken = await requireAccessToken();
  const { id } = await params;
  const { companyId: queryCompanyId } = await searchParams;

  let sparePart;
  try {
    sparePart = await getSparePartById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  const companyId = queryCompanyId ?? sparePart.companyId;

  const [inWarehouseAssets, orgUnits] = await Promise.all([
    sparePart.status === "InStock" ? getAssets(accessToken, { companyId, status: "InWarehouse", pageSize: 200 }) : null,
    sparePart.status === "Installed" ? getOrgUnitTree(accessToken, companyId) : null,
  ]);
  const warehouses = orgUnits?.filter((o) => o.orgUnitTypeName === "Almacén") ?? [];

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4 p-8">
      <AppHeader title="Refacciones" />
      <div className="mb-2 flex justify-end">
        <Button variant="outline" render={<Link href={`/spare-parts?companyId=${companyId}`} />}>
          ← Volver
        </Button>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold tracking-tight">{sparePart.name}</h2>
          <p className="text-muted-foreground font-mono text-sm">{sparePart.serialNumber}</p>
        </div>
        <Badge variant={sparePartStatusBadgeVariant(sparePart.status)}>{SPARE_PART_STATUS_LABELS[sparePart.status]}</Badge>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">Acciones</CardTitle>
        </CardHeader>
        <CardContent>
          {sparePart.status === "InStock" && inWarehouseAssets && (
            <div className="flex flex-col gap-3">
              <InstallSparePartForm sparePartId={sparePart.id} assets={inWarehouseAssets.items} />
              <form action={disposeSparePartAction.bind(null, sparePart.id)}>
                <Button type="submit" size="sm" variant="outline" className="text-destructive">
                  Dar de baja
                </Button>
              </form>
            </div>
          )}
          {sparePart.status === "Installed" && (
            <UninstallSparePartForm sparePartId={sparePart.id} warehouses={warehouses} />
          )}
          {sparePart.status === "Disposed" && (
            <p className="text-muted-foreground text-sm">Esta refacción ya fue dada de baja.</p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">Historial de instalación / retiro</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          {sparePart.installations.length === 0 ? (
            <p className="text-muted-foreground text-sm">Sin historial todavía.</p>
          ) : (
            sparePart.installations.map((i, index) => (
              <div key={index} className="border-b pb-2 last:border-b-0 last:pb-0">
                <p className="text-sm">
                  <span className="font-medium">{i.assetFolio}</span>
                  {" — "}
                  {new Date(i.installedAtUtc).toLocaleDateString("es-MX")}
                  {" → "}
                  {i.removedAtUtc ? new Date(i.removedAtUtc).toLocaleDateString("es-MX") : "actualmente instalada"}
                </p>
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}
