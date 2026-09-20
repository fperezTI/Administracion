import type { ApprovalInstanceStatus, ApprovalMode } from "@/lib/api";

export const APPROVAL_STATUS_LABELS: Record<ApprovalInstanceStatus, string> = {
  Pending: "Pendiente",
  Approved: "Aprobada",
  Rejected: "Rechazada",
  Cancelled: "Cancelada",
};

export const APPROVAL_MODE_LABELS: Record<ApprovalMode, string> = {
  Sequential: "Secuencial",
  Parallel: "Paralelo",
};

export function approvalStatusBadgeVariant(status: ApprovalInstanceStatus): "success" | "warning" | "destructive" | "outline" {
  switch (status) {
    case "Approved":
      return "success";
    case "Pending":
      return "warning";
    case "Rejected":
      return "destructive";
    default:
      return "outline";
  }
}

/** Turns a Movement/Approval ContextType into a plain label + link — the two consumers F4 ships
 * (asset.decommission, asset.disposal) encode their target in the string (see AssetApprovalReactionHandler),
 * so this is the one place that knows how to read it back for display. */
export function describeApprovalContext(contextType: string): string {
  if (contextType === "AssetDecommission") {
    return "Baja de activo";
  }
  if (contextType.startsWith("AssetDisposal:")) {
    const target = contextType.slice("AssetDisposal:".length);
    const targetLabels: Record<string, string> = { Sold: "venta", Donated: "donación", Destroyed: "destrucción" };
    return `Disposición de activo (${targetLabels[target] ?? target})`;
  }
  if (contextType === "InternalRequest") {
    return "Solicitud interna";
  }
  return contextType;
}
