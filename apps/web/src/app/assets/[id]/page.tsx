import Link from "next/link";
import { notFound } from "next/navigation";
import { revalidatePath } from "next/cache";
import {
  Archive,
  ArrowLeftRight,
  MapPin,
  Pencil,
  Plus,
  Printer,
  QrCode,
  Repeat,
  Trash2,
  Unlink,
  UserPlus,
  Wrench,
} from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssetById, getAssetCategoryById, getAssignments, getMe, getMovements, unlinkAssetAccessory } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Breadcrumbs } from "@/components/layout/breadcrumbs";
import { DocumentsPanel } from "@/components/documents-panel";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import {
  ASSET_STATUS_LABELS,
  IDENTIFICATION_TECHNOLOGY_LABELS,
  PHYSICAL_CONDITION_LABELS,
  assetStatusBadgeVariant,
  canRequestDecommission,
} from "@/lib/asset-labels";
import { formatDateTime, resolveTimeZone } from "@/lib/format-date";
import { MOVEMENT_TYPE_LABELS } from "@/lib/inventory-labels";

export default async function AssetDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let asset;
  try {
    asset = await getAssetById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  const me = await getMe(accessToken);
  const timeZone = resolveTimeZone(me.companies, asset.companyId);
  const category = await getAssetCategoryById(accessToken, asset.assetCategoryId).catch(() => null);
  const fieldNameById = new Map((category?.customFields ?? []).map((f) => [f.id, f.name]));
  const movements = await getMovements(accessToken, { companyId: asset.companyId, assetId: id, pageSize: 20 }).catch(
    () => null,
  );

  const activeAssignment =
    asset.status === "Assigned"
      ? await getAssignments(accessToken, {
          companyId: asset.companyId,
          assetId: id,
          status: "Accepted",
          pageSize: 1,
        })
          .then((result) => result.items[0] ?? null)
          .catch(() => null)
      : null;

  async function unlinkAccessory(accessoryAssetId: string) {
    "use server";
    const token = await requireAccessToken();
    await unlinkAssetAccessory(token, id, accessoryAssetId);
    revalidatePath(`/assets/${id}`);
  }

  return (
    <>
      <AppHeader title="Activos" />
    <div className="mx-auto flex max-w-3xl flex-col gap-4 px-8 pb-8">
      <Breadcrumbs items={[{ label: "Activos", href: "/assets" }, { label: asset.internalFolio }]} />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="font-mono text-lg font-semibold tracking-tight">{asset.internalFolio}</h2>
          <p className="text-muted-foreground text-sm">
            {asset.brand} {asset.model} — {category?.name ?? "Categoría"}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={assetStatusBadgeVariant(asset.status)}>{ASSET_STATUS_LABELS[asset.status]}</Badge>
          <Button variant="outline" render={<Link href={`/assets/${id}/label`} />}>
            <QrCode data-icon="inline-start" />
            Ver etiqueta
          </Button>
          <Button variant="outline" render={<Link href={`/assets/${id}/edit`} />}>
            <Pencil data-icon="inline-start" />
            Editar
          </Button>
          <Button variant="outline" render={<Link href={`/assets/${id}/relocate`} />}>
            <MapPin data-icon="inline-start" />
            Reubicar
          </Button>
          {asset.accessoryOfAssetId === null && (
            <Button variant="outline" render={<Link href={`/assets/${id}/link-accessory`} />}>
              <Plus data-icon="inline-start" />
              Vincular accesorio
            </Button>
          )}
          {asset.status === "InWarehouse" && (
            <Button
              variant="outline"
              render={<Link href={`/assignments/new?companyId=${asset.companyId}&assetId=${id}`} />}
            >
              <UserPlus data-icon="inline-start" />
              Asignar
            </Button>
          )}
          {activeAssignment && (
            <>
              <Button variant="outline" render={<Link href={`/assets/${id}/reassign`} />}>
                <Repeat data-icon="inline-start" />
                Reasignar
              </Button>
              <Button
                variant="outline"
                render={<Link href={`/assignments/${activeAssignment.id}/resguardo`} />}
              >
                <Printer data-icon="inline-start" />
                Imprimir resguardo
              </Button>
            </>
          )}
          {asset.status === "InWarehouse" && (
            <Button variant="outline" render={<Link href={`/transfers/new?companyId=${asset.companyId}`} />}>
              <ArrowLeftRight data-icon="inline-start" />
              Solicitar transferencia
            </Button>
          )}
          {(asset.status === "InWarehouse" || asset.status === "Assigned") && (
            <Button
              variant="outline"
              render={<Link href={`/maintenance-orders/new?companyId=${asset.companyId}&assetId=${id}`} />}
            >
              <Wrench data-icon="inline-start" />
              Abrir orden de mantenimiento
            </Button>
          )}
          {canRequestDecommission(asset.status) && (
            <Button variant="outline" render={<Link href={`/assets/${id}/decommission`} />}>
              <Archive data-icon="inline-start" />
              Solicitar baja
            </Button>
          )}
          {asset.status === "Decommissioned" && (
            <Button variant="outline" render={<Link href={`/assets/${id}/dispose`} />}>
              <Trash2 data-icon="inline-start" />
              Solicitar disposición
            </Button>
          )}
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">General</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-2 text-sm">
          <Field label="Folio patrimonial" value={asset.patrimonialFolio} />
          <Field label="Número de serie" value={asset.serialNumber} />
          <Field label="Condición física" value={PHYSICAL_CONDITION_LABELS[asset.physicalCondition]} />
          <Field label="Descripción" value={asset.description} />
        </CardContent>
      </Card>

      {asset.accessoryOfAssetId && (
        <p className="text-muted-foreground text-sm">
          Es accesorio de{" "}
          <Link href={`/assets/${asset.accessoryOfAssetId}`} className="hover:text-primary underline">
            {asset.accessoryOfAssetFolio}
          </Link>
          .
        </p>
      )}

      {asset.accessories.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Accesorios vinculados</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-2">
            {asset.accessories.map((accessory) => (
              <div key={accessory.id} className="flex items-center justify-between gap-2 text-sm">
                <div>
                  <Link href={`/assets/${accessory.id}`} className="hover:text-primary font-mono hover:underline">
                    {accessory.internalFolio}
                  </Link>
                  <span className="text-muted-foreground">
                    {" "}
                    — {accessory.brand} {accessory.model} — {ASSET_STATUS_LABELS[accessory.status]}
                  </span>
                </div>
                <form action={unlinkAccessory.bind(null, accessory.id)}>
                  <Button variant="outline" size="sm" type="submit">
                    <Unlink data-icon="inline-start" />
                    Quitar
                  </Button>
                </form>
              </div>
            ))}
          </CardContent>
        </Card>
      )}

      {asset.tag && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Identificación</CardTitle>
          </CardHeader>
          <CardContent className="flex items-center justify-between text-sm">
            <div className="grid gap-1">
              <Field label="Tecnología" value={IDENTIFICATION_TECHNOLOGY_LABELS[asset.tag.technology]} />
              <Field label="Código" value={asset.tag.code} mono />
              <Field label="Veces impresa" value={String(asset.tag.printCount)} />
            </div>
            <Button variant="outline" render={<Link href={`/assets/${id}/label`} />}>
              <QrCode data-icon="inline-start" />
              Ver etiqueta
            </Button>
          </CardContent>
        </Card>
      )}

      {(asset.acquisitionDate || asset.acquisitionCost || asset.supplier || asset.invoice || asset.purchaseOrder) && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Información financiera (informativa)</CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-2 text-sm">
            <Field label="Fecha de adquisición" value={asset.acquisitionDate} />
            <Field
              label="Costo"
              value={asset.acquisitionCost != null ? `${asset.acquisitionCost} ${asset.currency ?? ""}` : null}
            />
            <Field label="Proveedor" value={asset.supplier} />
            <Field label="Factura" value={asset.invoice} />
            <Field label="Orden de compra" value={asset.purchaseOrder} />
          </CardContent>
        </Card>
      )}

      {(asset.warrantyStartDate || asset.warrantyEndDate || asset.supportContract || asset.supportProvider) && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Garantía y soporte</CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-2 text-sm">
            <Field label="Inicio de garantía" value={asset.warrantyStartDate} />
            <Field label="Fin de garantía" value={asset.warrantyEndDate} />
            <Field label="Contrato de soporte" value={asset.supportContract} />
            <Field label="Proveedor de soporte" value={asset.supportProvider} />
          </CardContent>
        </Card>
      )}

      {asset.customFieldValues.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Campos técnicos ({category?.name})</CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-2 text-sm">
            {asset.customFieldValues.map((v) => (
              <Field
                key={v.customFieldDefinitionId}
                label={fieldNameById.get(v.customFieldDefinitionId) ?? "Campo"}
                value={v.value}
              />
            ))}
          </CardContent>
        </Card>
      )}

      {movements && movements.items.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Historial de movimientos</CardTitle>
          </CardHeader>
          <CardContent className="px-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Folio</TableHead>
                  <TableHead>Tipo</TableHead>
                  <TableHead>Fecha</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {movements.items.map((m) => (
                  <TableRow key={m.id}>
                    <TableCell className="font-mono">{m.folioNumber}</TableCell>
                    <TableCell>{MOVEMENT_TYPE_LABELS[m.type]}</TableCell>
                    <TableCell>{formatDateTime(m.effectiveAtUtc, timeZone)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      <DocumentsPanel accessToken={accessToken} entityType="Asset" entityId={id} revalidatePathTarget={`/assets/${id}`} />
    </div>
    </>
  );
}

function Field({ label, value, mono }: { label: string; value: string | null | undefined; mono?: boolean }) {
  return (
    <div>
      <p className="text-muted-foreground text-xs">{label}</p>
      <p className={mono ? "font-mono" : undefined}>{value ?? "—"}</p>
    </div>
  );
}
