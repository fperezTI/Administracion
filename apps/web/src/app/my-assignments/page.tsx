import Link from "next/link";
import { X } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMyAssignments, type MyAssignmentSummary } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { ASSIGNMENT_STATUS_LABELS, assignmentStatusBadgeVariant } from "@/lib/inventory-labels";
import { rejectAssignmentAction } from "./actions";
import { SignAssignmentForm } from "./sign-assignment-form";

function groupAssignments(assignments: MyAssignmentSummary[]): MyAssignmentSummary[][] {
  const groups = new Map<string, MyAssignmentSummary[]>();
  for (const assignment of assignments) {
    const key = assignment.groupId ?? assignment.id;
    const group = groups.get(key);
    if (group) {
      group.push(assignment);
    } else {
      groups.set(key, [assignment]);
    }
  }
  return [...groups.values()];
}

export default async function MyAssignmentsPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const assignments = await getMyAssignments(accessToken);
    const groups = groupAssignments(assignments);

    content =
      groups.length === 0 ? (
        <p className="text-muted-foreground text-sm">No tienes activos asignados.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {groups.map((group) => {
            const [first, ...rest] = group;
            return (
              <Card key={first.id}>
                <CardContent className="flex flex-col gap-3">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div className="flex flex-col gap-1">
                      {group.map((a) => (
                        <div key={a.id}>
                          <Link href={`/assets/${a.assetId}`} className="hover:text-primary font-mono font-medium hover:underline">
                            {a.assetFolio}
                          </Link>
                          <span className="text-muted-foreground text-sm">
                            {" "}
                            — {a.assetBrand} {a.assetModel}
                          </span>
                        </div>
                      ))}
                      {rest.length > 0 && (
                        <p className="text-muted-foreground text-xs">Paquete de {group.length} activos</p>
                      )}
                      <Link href={`/my-assignments/${first.id}`} className="text-primary text-xs hover:underline">
                        Ver detalle
                      </Link>
                    </div>
                    <Badge variant={assignmentStatusBadgeVariant(first.status)}>
                      {ASSIGNMENT_STATUS_LABELS[first.status]}
                    </Badge>
                  </div>
                  {first.status === "PendingSignature" && (
                    <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
                      <SignAssignmentForm assignmentId={first.id} />
                      <form action={rejectAssignmentAction.bind(null, first.id)}>
                        <Button type="submit" variant="outline" size="sm">
                          <X data-icon="inline-start" />
                          Rechazar
                        </Button>
                      </form>
                    </div>
                  )}
                </CardContent>
              </Card>
            );
          })}
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
