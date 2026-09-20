import type { ImportBatchStatus, ImportCommitMode } from "@/lib/api";

export const IMPORT_BATCH_STATUS_LABELS: Record<ImportBatchStatus, string> = {
  Queued: "En cola",
  Validating: "Validando",
  Validated: "Validado",
  Processing: "Procesando",
  Completed: "Completado",
  CompletedWithErrors: "Completado con errores",
  Failed: "Falló",
  Cancelled: "Cancelado",
};

export function importBatchStatusBadgeVariant(
  status: ImportBatchStatus,
): "success" | "warning" | "destructive" | "outline" {
  switch (status) {
    case "Completed":
      return "success";
    case "Queued":
    case "Validating":
    case "Processing":
      return "warning";
    case "Failed":
    case "CompletedWithErrors":
      return "destructive";
    default:
      return "outline";
  }
}

export const IMPORT_COMMIT_MODE_LABELS: Record<ImportCommitMode, string> = {
  AllOrNothing: "Todo o nada",
  ValidRowsOnly: "Solo filas válidas",
};
