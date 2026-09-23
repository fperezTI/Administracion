import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getInternalRequestById, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { formatDateTime, resolveTimeZone } from "@/lib/format-date";
import { INTERNAL_REQUEST_STATUS_LABELS, INTERNAL_REQUEST_TYPE_LABELS, internalRequestStatusBadgeVariant } from "@/lib/request-labels";

export default async function InternalRequestDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let request;
  try {
    request = await getInternalRequestById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  const me = await getMe(accessToken);
  const timeZone = resolveTimeZone(me.companies, request.companyId);

  return (
    <>
      <AppHeader title="Solicitudes internas" />
    <div className="mx-auto flex max-w-3xl flex-col gap-4 px-8 pb-8">
      <div className="mb-2 flex justify-end">
        <Button variant="outline" render={<Link href="/requests" />}>
          <ArrowLeft data-icon="inline-start" />
          Volver
        </Button>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="font-mono text-lg font-semibold tracking-tight">{request.assetFolio}</h2>
          <p className="text-muted-foreground text-sm">
            {INTERNAL_REQUEST_TYPE_LABELS[request.type]} — solicitada por {request.requestedByDisplayName}
          </p>
        </div>
        <Badge variant={internalRequestStatusBadgeVariant(request.status)}>{INTERNAL_REQUEST_STATUS_LABELS[request.status]}</Badge>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">Justificación</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-2">
          <p className="text-sm whitespace-pre-wrap">{request.justification}</p>
          {request.expectedReturnDate && (
            <p className="text-muted-foreground text-xs">Devolución esperada: {request.expectedReturnDate}</p>
          )}
          <p className="text-muted-foreground text-xs">
            Solicitada el {formatDateTime(request.requestedAtUtc, timeZone)}
            {request.decidedAtUtc && ` — decidida el ${formatDateTime(request.decidedAtUtc, timeZone)}`}
          </p>
        </CardContent>
      </Card>
    </div>
    </>
  );
}
