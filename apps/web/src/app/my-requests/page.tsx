import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMyInternalRequests } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { INTERNAL_REQUEST_STATUS_LABELS, INTERNAL_REQUEST_TYPE_LABELS, internalRequestStatusBadgeVariant } from "@/lib/request-labels";
import { cancelMyRequestAction } from "./actions";

export default async function MyRequestsPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const requests = await getMyInternalRequests(accessToken);

    content =
      requests.length === 0 ? (
        <p className="text-muted-foreground text-sm">No has hecho ninguna solicitud todavía.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {requests.map((r) => (
            <Card key={r.id}>
              <CardContent className="flex flex-col gap-3">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <Link href={`/assets/${r.assetId}`} className="hover:text-primary font-mono font-medium hover:underline">
                      {r.assetFolio}
                    </Link>
                    <p className="text-muted-foreground text-sm">{INTERNAL_REQUEST_TYPE_LABELS[r.type]}</p>
                  </div>
                  <Badge variant={internalRequestStatusBadgeVariant(r.status)}>{INTERNAL_REQUEST_STATUS_LABELS[r.status]}</Badge>
                </div>
                <p className="text-sm">{r.justification}</p>
                {r.expectedReturnDate && (
                  <p className="text-muted-foreground text-xs">Devolución esperada: {r.expectedReturnDate}</p>
                )}
                {r.status === "PendingApproval" && (
                  <form action={cancelMyRequestAction.bind(null, r.id)} className="flex justify-end">
                    <Button type="submit" size="sm" variant="outline">
                      Cancelar
                    </Button>
                  </form>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No fue posible consultar tus solicitudes." : "No fue posible consultar tus solicitudes."}
      </p>
    );
  }

  return (
    <>
      <AppHeader title="Mis solicitudes" subtitle="Solicitudes internas que has hecho — asignación, préstamo o mantenimiento." />
    <div className="mx-auto max-w-3xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href="/requests/new" />}>Nueva solicitud</Button>
      </div>
      {content}
    </div>
    </>
  );
}
