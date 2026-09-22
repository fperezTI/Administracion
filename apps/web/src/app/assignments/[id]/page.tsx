import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssignmentById, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Breadcrumbs } from "@/components/layout/breadcrumbs";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { formatDateTime, resolveTimeZone } from "@/lib/format-date";
import { ASSIGNMENT_STATUS_LABELS, assignmentStatusBadgeVariant } from "@/lib/inventory-labels";
import { cancelAssignmentAction } from "./actions";
import { ReturnAssignmentForm } from "./return-assignment-form";

export default async function AssignmentDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let assignment;
  try {
    assignment = await getAssignmentById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  const me = await getMe(accessToken);
  const timeZone = resolveTimeZone(me.companies, assignment.companyId);

  return (
    <>
      <AppHeader title="Asignación" />
    <div className="mx-auto flex max-w-3xl flex-col gap-4 px-8 pb-8">
      <Breadcrumbs items={[{ label: "Asignaciones", href: "/assignments" }, { label: assignment.assetFolio }]} />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <Link href={`/assets/${assignment.assetId}`} className="hover:text-primary font-mono text-lg font-semibold hover:underline">
            {assignment.assetFolio}
          </Link>
          <p className="text-muted-foreground text-sm">Asignado a {assignment.assignedToDisplayName}</p>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={assignmentStatusBadgeVariant(assignment.status)}>{ASSIGNMENT_STATUS_LABELS[assignment.status]}</Badge>
          {assignment.status !== "Cancelled" && (
            <Button variant="outline" render={<Link href={`/assignments/${assignment.id}/resguardo`} />}>
              Imprimir resguardo
            </Button>
          )}
        </div>
      </div>

      {assignment.groupMembers.length > 1 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Incluye también</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-1 text-sm">
            {assignment.groupMembers
              .filter((m) => m.assetId !== assignment.assetId)
              .map((member) => (
                <Link key={member.assetId} href={`/assets/${member.assetId}`} className="hover:text-primary hover:underline">
                  <span className="font-mono">{member.assetFolio}</span> — {member.brand} {member.model}
                  {!member.isPrimary && <span className="text-muted-foreground"> (accesorio)</span>}
                </Link>
              ))}
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">Firma de recepción</CardTitle>
        </CardHeader>
        <CardContent className="text-sm">
          {assignment.acceptanceSignature ? (
            <div className="grid gap-1">
              <p>{assignment.acceptanceSignature.signerDisplayName}</p>
              <p className="text-muted-foreground text-xs">
                {formatDateTime(assignment.acceptanceSignature.signedAtUtc, timeZone)}
              </p>
              <p className="font-mono text-xs break-all">{assignment.acceptanceSignature.contentHash}</p>
            </div>
          ) : (
            <p className="text-muted-foreground">
              El destinatario todavía no confirma la recepción desde &ldquo;Mis asignaciones&rdquo;.
            </p>
          )}
        </CardContent>
      </Card>

      {assignment.returnSignature && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Firma de devolución</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">
            <div className="grid gap-1">
              <p>{assignment.returnSignature.signerDisplayName}</p>
              <p className="text-muted-foreground text-xs">
                {formatDateTime(assignment.returnSignature.signedAtUtc, timeZone)}
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {assignment.status === "PendingSignature" && (
        <form action={cancelAssignmentAction.bind(null, assignment.id)}>
          <Button variant="outline" type="submit">
            Cancelar asignación
          </Button>
        </form>
      )}

      {assignment.status === "Accepted" && <ReturnAssignmentForm assignmentId={assignment.id} />}
    </div>
    </>
  );
}
