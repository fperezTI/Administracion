import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getTransferById } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Breadcrumbs } from "@/components/layout/breadcrumbs";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { TRANSFER_STATUS_LABELS, transferStatusBadgeVariant } from "@/lib/transfer-labels";
import { cancelTransferAction } from "./actions";
import { ReceiveTransferForm } from "./receive-transfer-form";

export default async function TransferDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let transfer;
  try {
    transfer = await getTransferById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  return (
    <>
      <AppHeader title="Transferencia" />
    <div className="mx-auto flex max-w-3xl flex-col gap-4 px-8 pb-8">
      <Breadcrumbs items={[{ label: "Transferencias", href: "/transfers" }, { label: transfer.assetFolio }]} />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <Link href={`/assets/${transfer.assetId}`} className="hover:text-primary font-mono text-lg font-semibold hover:underline">
            {transfer.assetFolio}
          </Link>
          <p className="text-muted-foreground text-sm">
            {transfer.fromCompanyName} → {transfer.toCompanyName}
          </p>
        </div>
        <Badge variant={transferStatusBadgeVariant(transfer.status)}>{TRANSFER_STATUS_LABELS[transfer.status]}</Badge>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 rounded-lg border p-4 text-sm">
        <div>
          <p className="text-muted-foreground text-xs">Solicitado por</p>
          <p>{transfer.requestedByDisplayName}</p>
        </div>
        <div>
          <p className="text-muted-foreground text-xs">Fecha de solicitud</p>
          <p>{new Date(transfer.requestedAtUtc).toLocaleString("es-MX")}</p>
        </div>
        {transfer.departedAtUtc && (
          <div>
            <p className="text-muted-foreground text-xs">Salió el</p>
            <p>{new Date(transfer.departedAtUtc).toLocaleString("es-MX")}</p>
          </div>
        )}
        {transfer.completedAtUtc && (
          <div>
            <p className="text-muted-foreground text-xs">Recibido el</p>
            <p>{new Date(transfer.completedAtUtc).toLocaleString("es-MX")}</p>
          </div>
        )}
        {transfer.notes && (
          <div className="col-span-2">
            <p className="text-muted-foreground text-xs">Justificación</p>
            <p>{transfer.notes}</p>
          </div>
        )}
      </div>

      {transfer.status === "PendingApproval" && (
        <form action={cancelTransferAction.bind(null, transfer.id)}>
          <Button variant="outline" type="submit">
            Cancelar solicitud
          </Button>
        </form>
      )}

      {transfer.status === "InTransit" && <ReceiveTransferForm transferId={transfer.id} />}
    </div>
    </>
  );
}
