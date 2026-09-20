import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMaintenanceOrderById } from "@/lib/api";
import { ASSET_STATUS_LABELS } from "@/lib/asset-labels";
import { MAINTENANCE_ORDER_STATUS_LABELS, MAINTENANCE_ORDER_TYPE_LABELS, maintenanceOrderStatusBadgeVariant } from "@/lib/maintenance-labels";
import { AppHeader } from "@/components/app-header";
import { DocumentsPanel } from "@/components/documents-panel";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { CloseMaintenanceOrderForm } from "./close-maintenance-order-form";

export default async function MaintenanceOrderDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let order;
  try {
    order = await getMaintenanceOrderById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4 p-8">
      <AppHeader title="Mantenimiento" />
      <div className="mb-2 flex justify-end">
        <Button variant="outline" render={<Link href={`/maintenance-orders?companyId=${order.companyId}`} />}>
          ← Volver
        </Button>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="font-mono text-lg font-semibold tracking-tight">{order.folio}</h2>
          <p className="text-muted-foreground text-sm">
            {order.assetFolio} — {MAINTENANCE_ORDER_TYPE_LABELS[order.type]}
          </p>
        </div>
        <Badge variant={maintenanceOrderStatusBadgeVariant(order.status)}>{MAINTENANCE_ORDER_STATUS_LABELS[order.status]}</Badge>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">Descripción</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm whitespace-pre-wrap">{order.description}</p>
          <p className="text-muted-foreground mt-2 text-xs">
            Abierta el {new Date(order.openedAtUtc).toLocaleString("es-MX")}
          </p>
        </CardContent>
      </Card>

      {order.status === "Open" ? (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Cerrar orden</CardTitle>
          </CardHeader>
          <CardContent>
            <CloseMaintenanceOrderForm maintenanceOrderId={order.id} checklistResults={order.checklistResults} />
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Resultado</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-3">
            <p className="text-sm">
              <span className="font-medium">{order.resultStatus ? ASSET_STATUS_LABELS[order.resultStatus] : "—"}</span>
              {" — "}
              {order.closedAtUtc && new Date(order.closedAtUtc).toLocaleString("es-MX")}
            </p>
            <p className="text-sm whitespace-pre-wrap">{order.resultNotes}</p>
            {order.checklistResults.length > 0 && (
              <ul className="list-inside list-disc text-sm">
                {order.checklistResults.map((item) => (
                  <li key={item.itemIndex}>
                    <span className={item.isCompleted ? "text-success" : "text-destructive"}>
                      {item.isCompleted ? "✓" : "✗"}
                    </span>{" "}
                    {item.itemText}
                    {item.notes && <span className="text-muted-foreground"> — {item.notes}</span>}
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>
      )}

      <DocumentsPanel
        accessToken={accessToken}
        entityType="MaintenanceOrder"
        entityId={id}
        revalidatePathTarget={`/maintenance-orders/${id}`}
      />
    </div>
  );
}
