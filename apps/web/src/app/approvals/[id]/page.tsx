import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getApprovalInstanceById, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Breadcrumbs } from "@/components/layout/breadcrumbs";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { APPROVAL_MODE_LABELS, APPROVAL_STATUS_LABELS, approvalStatusBadgeVariant, describeApprovalContext } from "@/lib/approval-labels";
import { formatDateTime, resolveTimeZone } from "@/lib/format-date";

export default async function ApprovalDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let approval;
  try {
    approval = await getApprovalInstanceById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  const me = await getMe(accessToken);
  // ApprovalInstanceDetail has no companyId exposed on this DTO — falls back to the viewer's first company.
  const timeZone = resolveTimeZone(me.companies, null);

  return (
    <>
      <AppHeader title="Aprobación" />
    <div className="mx-auto flex max-w-3xl flex-col gap-4 px-8 pb-8">
      <Breadcrumbs
        items={[{ label: "Mis aprobaciones", href: "/my-approvals" }, { label: describeApprovalContext(approval.contextType) }]}
      />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold tracking-tight">{describeApprovalContext(approval.contextType)}</h2>
          <p className="text-muted-foreground text-sm">
            Solicitado por {approval.requestedByDisplayName} — {APPROVAL_MODE_LABELS[approval.mode]}, {approval.requiredApprovals}{" "}
            aprobación{approval.requiredApprovals === 1 ? "" : "es"} requerida{approval.requiredApprovals === 1 ? "" : "s"}
          </p>
        </div>
        <Badge variant={approvalStatusBadgeVariant(approval.status)}>{APPROVAL_STATUS_LABELS[approval.status]}</Badge>
      </div>

      {approval.comment && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Justificación</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">{approval.comment}</CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">Decisiones</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-3 text-sm">
          {approval.steps.length === 0 ? (
            <p className="text-muted-foreground">Todavía no hay decisiones registradas.</p>
          ) : (
            approval.steps.map((step) => (
              <div key={step.approverUserId} className="border-b pb-2 last:border-b-0 last:pb-0">
                <p>
                  <span className="font-medium">{step.approverDisplayName}</span> —{" "}
                  {step.decision === "Approved" ? "Aprobó" : "Rechazó"} el{" "}
                  {formatDateTime(step.decidedAtUtc, timeZone)}
                </p>
                {step.comment && <p className="text-muted-foreground">&ldquo;{step.comment}&rdquo;</p>}
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </div>
    </>
  );
}
