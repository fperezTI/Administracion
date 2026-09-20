import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMyPendingApprovals } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Card, CardContent } from "@/components/ui/card";
import { describeApprovalContext } from "@/lib/approval-labels";
import { DecideApprovalForm } from "./decide-approval-form";

export default async function MyApprovalsPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const approvals = await getMyPendingApprovals(accessToken);

    content =
      approvals.length === 0 ? (
        <p className="text-muted-foreground text-sm">No tienes aprobaciones pendientes.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {approvals.map((a) => (
            <Card key={a.id}>
              <CardContent className="flex flex-col gap-3">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <p className="font-medium">{describeApprovalContext(a.contextType)}</p>
                    <p className="text-muted-foreground text-sm">
                      Solicitado por {a.requestedByDisplayName} — {new Date(a.createdAtUtc).toLocaleDateString("es-MX")}
                    </p>
                    {a.comment && <p className="text-sm">&ldquo;{a.comment}&rdquo;</p>}
                  </div>
                  <Link href={`/approvals/${a.id}`} className="text-primary text-sm hover:underline">
                    Ver detalle
                  </Link>
                </div>
                <DecideApprovalForm approvalInstanceId={a.id} />
              </CardContent>
            </Card>
          ))}
        </div>
      );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No fue posible consultar tus aprobaciones." : "No fue posible consultar tus aprobaciones."}
      </p>
    );
  }

  return (
    <div className="mx-auto max-w-2xl p-8">
      <AppHeader title="Mis aprobaciones" subtitle="Solicitudes pendientes donde tienes un rol elegible para decidir." />
      {content}
    </div>
  );
}
