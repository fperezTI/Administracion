import type { TransferStatus } from "@/lib/api";

export const TRANSFER_STATUS_LABELS: Record<TransferStatus, string> = {
  PendingApproval: "Pendiente de aprobación",
  Rejected: "Rechazada",
  Cancelled: "Cancelada",
  InTransit: "En tránsito",
  Completed: "Completada",
};

export function transferStatusBadgeVariant(status: TransferStatus): "success" | "warning" | "destructive" | "outline" {
  switch (status) {
    case "Completed":
      return "success";
    case "PendingApproval":
    case "InTransit":
      return "warning";
    case "Rejected":
      return "destructive";
    default:
      return "outline";
  }
}
