import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMyAssignments } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { ASSIGNMENT_STATUS_LABELS, assignmentStatusBadgeVariant } from "@/lib/inventory-labels";
import { SignAssignmentForm } from "./sign-assignment-form";

export default async function MyAssignmentsPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const assignments = await getMyAssignments(accessToken);

    content =
      assignments.length === 0 ? (
        <p className="text-muted-foreground text-sm">No tienes activos asignados.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {assignments.map((a) => (
            <Card key={a.id}>
              <CardContent className="flex flex-col gap-3">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <Link href={`/assets/${a.assetId}`} className="hover:text-primary font-mono font-medium hover:underline">
                      {a.assetFolio}
                    </Link>
                    <p className="text-muted-foreground text-sm">
                      {a.assetBrand} {a.assetModel}
                    </p>
                  </div>
                  <Badge variant={assignmentStatusBadgeVariant(a.status)}>{ASSIGNMENT_STATUS_LABELS[a.status]}</Badge>
                </div>
                {a.status === "PendingSignature" && <SignAssignmentForm assignmentId={a.id} />}
              </CardContent>
            </Card>
          ))}
        </div>
      );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No fue posible consultar tus asignaciones." : "No fue posible consultar tus asignaciones."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Mis asignaciones"
        subtitle="Activos que tienes bajo tu resguardo — confirma la recepción de los pendientes."
      />
    <div className="mx-auto max-w-3xl px-8 pb-8">
      {content}
    </div>
    </>
  );
}
