import type { InternalRequestStatus, InternalRequestType } from "@/lib/api";

export const INTERNAL_REQUEST_TYPE_LABELS: Record<InternalRequestType, string> = {
  AssetAssignment: "Asignación de activo",
  Loan: "Préstamo",
  Maintenance: "Mantenimiento",
};

export const INTERNAL_REQUEST_STATUS_LABELS: Record<InternalRequestStatus, string> = {
  PendingApproval: "Pendiente de aprobación",
  Rejected: "Rechazada",
  Cancelled: "Cancelada",
  Fulfilled: "Cumplida",
};

export function internalRequestStatusBadgeVariant(status: InternalRequestStatus): "success" | "warning" | "destructive" | "outline" {
  switch (status) {
    case "Fulfilled":
      return "success";
    case "PendingApproval":
      return "warning";
    case "Rejected":
      return "destructive";
    default:
      return "outline";
  }
}
