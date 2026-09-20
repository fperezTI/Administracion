import type { AssignmentStatus, LoanStatus, MovementType } from "@/lib/api";

export const ASSIGNMENT_STATUS_LABELS: Record<AssignmentStatus, string> = {
  PendingSignature: "Pendiente de firma",
  Accepted: "Aceptada",
  Returned: "Devuelta",
  Cancelled: "Cancelada",
};

export const LOAN_STATUS_LABELS: Record<LoanStatus, string> = {
  Active: "Activo",
  Returned: "Devuelto",
};

export const MOVEMENT_TYPE_LABELS: Record<MovementType, string> = {
  Assignment: "Asignación",
  AssignmentReturn: "Devolución de asignación",
  Loan: "Préstamo",
  LoanReturn: "Devolución de préstamo",
  Relocation: "Reubicación",
};

export function assignmentStatusBadgeVariant(status: AssignmentStatus): "success" | "warning" | "outline" {
  switch (status) {
    case "Accepted":
      return "success";
    case "PendingSignature":
      return "warning";
    default:
      return "outline";
  }
}

export function loanStatusBadgeVariant(status: LoanStatus): "success" | "outline" {
  return status === "Active" ? "success" : "outline";
}
