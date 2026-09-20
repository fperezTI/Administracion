import type { AssetStatus, CustomFieldDataType, IdentificationTechnology, PhysicalCondition } from "@/lib/api";

export const ASSET_STATUS_LABELS: Record<AssetStatus, string> = {
  InWarehouse: "En almacén",
  Reserved: "Reservado",
  Assigned: "Asignado",
  OnLoan: "Prestado",
  InTransit: "En tránsito",
  InMaintenance: "En mantenimiento",
  UnderWarranty: "En garantía",
  Damaged: "Dañado",
  Lost: "Extraviado",
  Stolen: "Robado",
  PendingDecommission: "Pendiente de baja",
  Decommissioned: "Dado de baja",
  Sold: "Vendido",
  Donated: "Donado",
  Destroyed: "Destruido",
};

export const PHYSICAL_CONDITION_LABELS: Record<PhysicalCondition, string> = {
  Excellent: "Excelente",
  Good: "Buena",
  Fair: "Regular",
  Poor: "Mala",
  Damaged: "Dañado",
};

export const IDENTIFICATION_TECHNOLOGY_LABELS: Record<IdentificationTechnology, string> = {
  Qr: "Código QR",
  Barcode: "Código de barras",
  QrAndBarcode: "QR y código de barras",
  Nfc: "NFC",
  Rfid: "RFID",
};

export const CUSTOM_FIELD_DATA_TYPE_LABELS: Record<CustomFieldDataType, string> = {
  Text: "Texto",
  Number: "Número",
  Date: "Fecha",
  Boolean: "Sí/No",
  Select: "Selección",
};

/**
 * Color de estado — un mismo significado en toda la app (tabla, detalle, filtros): verde = en
 * circulación y sano, ámbar = en tránsito o requiere atención, rojo = riesgo o pérdida, neutro =
 * cerrado/histórico. No es decorativo: refleja la semántica operativa real de `AssetStatus`.
 */
export function assetStatusBadgeVariant(
  status: AssetStatus,
): "success" | "warning" | "destructive" | "outline" {
  switch (status) {
    case "InWarehouse":
    case "Assigned":
    case "OnLoan":
    case "UnderWarranty":
      return "success";
    case "Reserved":
    case "InTransit":
    case "InMaintenance":
    case "PendingDecommission":
      return "warning";
    case "Damaged":
    case "Lost":
    case "Stolen":
      return "destructive";
    default:
      return "outline";
  }
}

/** States AssetStateMachine actually allows to move into PendingDecommission (see F4's
 * RequestAssetDecommissionCommand) — kept in sync with the backend graph by hand, same as every other
 * frontend copy of a backend rule in this app (there is no shared-package boundary yet). */
const DECOMMISSION_ELIGIBLE_STATUSES: ReadonlySet<AssetStatus> = new Set([
  "InWarehouse",
  "Assigned",
  "InMaintenance",
  "UnderWarranty",
  "Damaged",
  "Lost",
  "Stolen",
]);

export function canRequestDecommission(status: AssetStatus): boolean {
  return DECOMMISSION_ELIGIBLE_STATUSES.has(status);
}
