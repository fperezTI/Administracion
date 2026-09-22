import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMyAssignmentById } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ASSIGNMENT_STATUS_LABELS, assignmentStatusBadgeVariant } from "@/lib/inventory-labels";
import { SignAssignmentForm } from "../sign-assignment-form";

export default async function MyAssignmentDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let assignment;
  try {
    assignment = await getMyAssignmentById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && (error.status === 404 || error.status === 403)) {
      notFound();
    }
    throw error;
  }

  return (
    <>
      <AppHeader title="Mi asignación" subtitle="Confirma la recepción del equipo que se te asignó." />
    <div className="mx-auto flex max-w-2xl flex-col gap-4 px-8 pb-8">
      <div className="flex items-center justify-between">
        <h2 className="font-mono text-lg font-semibold tracking-tight">{assignment.assetFolio}</h2>
        <Badge variant={assignmentStatusBadgeVariant(assignment.status)}>
          {ASSIGNMENT_STATUS_LABELS[assignment.status]}
        </Badge>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">
            {assignment.groupMembers.length > 1 ? "Activos incluidos" : "Activo"}
          </CardTitle>
        </CardHeader>
        <CardContent className="px-0">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="text-muted-foreground text-xs">
                <th className="px-6 py-1 font-medium">Folio</th>
                <th className="px-6 py-1 font-medium">Marca / modelo</th>
                <th className="px-6 py-1 font-medium">Serie</th>
                {assignment.groupMembers.length > 1 && <th className="px-6 py-1 font-medium">Tipo</th>}
              </tr>
            </thead>
            <tbody>
              {assignment.groupMembers.map((member) => (
                <tr key={member.assetId} className="border-t">
                  <td className="px-6 py-2 font-mono">{member.assetFolio}</td>
                  <td className="px-6 py-2">
                    {member.brand} {member.model}
                  </td>
                  <td className="px-6 py-2 font-mono">{member.serialNumber ?? "—"}</td>
                  {assignment.groupMembers.length > 1 && (
                    <td className="px-6 py-2">{member.isPrimary ? "Principal" : "Accesorio"}</td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </CardContent>
      </Card>

      {assignment.status === "PendingSignature" ? (
        <Card>
          <CardContent>
            <SignAssignmentForm assignmentId={assignment.id} />
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardContent className="text-sm">
            {assignment.acceptanceSignature ? (
              <p className="text-success font-medium">
                Ya confirmaste la recepción el{" "}
                {new Date(assignment.acceptanceSignature.signedAtUtc).toLocaleString("es-MX")}.
              </p>
            ) : (
              <p className="text-muted-foreground">
                Esta asignación ya no está pendiente de confirmación
                (estado: {ASSIGNMENT_STATUS_LABELS[assignment.status]}).
              </p>
            )}
          </CardContent>
        </Card>
      )}

      <Button variant="outline" render={<Link href="/my-assignments" />} className="self-start">
        ← Volver a mis asignaciones
      </Button>
    </div>
    </>
  );
}
