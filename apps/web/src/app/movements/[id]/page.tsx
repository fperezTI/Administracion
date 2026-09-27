import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMe, getMovementById } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Breadcrumbs } from "@/components/layout/breadcrumbs";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { formatDateTime, resolveTimeZone } from "@/lib/format-date";
import { MOVEMENT_STATUS_LABELS, MOVEMENT_TYPE_LABELS, movementStatusBadgeVariant } from "@/lib/inventory-labels";

export default async function MovementDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let movement;
  try {
    movement = await getMovementById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  const me = await getMe(accessToken);
  const timeZone = resolveTimeZone(me.companies, movement.companyId);

  return (
    <>
      <AppHeader title="Movimiento" />
    <div className="mx-auto flex max-w-3xl flex-col gap-4 px-8 pb-8">
      <Breadcrumbs items={[{ label: "Movimientos", href: "/movements" }, { label: movement.folioNumber }]} />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="font-mono text-lg font-semibold tracking-tight">{movement.folioNumber}</h2>
          <Link href={`/assets/${movement.assetId}`} className="hover:text-primary text-muted-foreground text-sm hover:underline">
            Volver al detalle del activo ({movement.assetFolio})
          </Link>
        </div>
        <Badge variant={movementStatusBadgeVariant(movement.status)}>{MOVEMENT_STATUS_LABELS[movement.status]}</Badge>
      </div>

      <Card>
        <CardContent className="grid grid-cols-2 gap-4">
          <Field label="Tipo" value={MOVEMENT_TYPE_LABELS[movement.type]} />
          <Field label="Fecha" value={formatDateTime(movement.effectiveAtUtc, timeZone)} />
          <Field label="De (área)" value={movement.fromOrgUnitName} />
          <Field label="A (área)" value={movement.toOrgUnitName} />
          <Field label="De (persona)" value={movement.fromUserDisplayName} />
          <Field label="A (persona)" value={movement.toUserDisplayName} />
          <div className="col-span-2">
            <Field label="Notas" value={movement.notes} />
          </div>
        </CardContent>
      </Card>
    </div>
    </>
  );
}

function Field({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <div>
      <p className="text-muted-foreground text-xs">{label}</p>
      <p>{value ?? "—"}</p>
    </div>
  );
}
