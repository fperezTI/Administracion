import type {
  ConsumableStockDirection,
  ConsumableStockMovementReason,
  MaintenanceOrderStatus,
  MaintenanceOrderType,
  SparePartStatus,
  WarrantyType,
} from "@/lib/api";

export const MAINTENANCE_ORDER_TYPE_LABELS: Record<MaintenanceOrderType, string> = {
  Preventive: "Preventivo",
  Corrective: "Correctivo",
};

export const MAINTENANCE_ORDER_STATUS_LABELS: Record<MaintenanceOrderStatus, string> = {
  Open: "Abierta",
  Closed: "Cerrada",
};

export function maintenanceOrderStatusBadgeVariant(status: MaintenanceOrderStatus): "success" | "warning" | "outline" {
  return status === "Open" ? "warning" : "success";
}

export const WARRANTY_TYPE_LABELS: Record<WarrantyType, string> = {
  Manufacturer: "Fabricante",
  Extended: "Extendida",
  ThirdParty: "Terceros",
};

export const SPARE_PART_STATUS_LABELS: Record<SparePartStatus, string> = {
  InStock: "En existencia",
  Installed: "Instalada",
  Disposed: "Dada de baja",
};

export function sparePartStatusBadgeVariant(status: SparePartStatus): "success" | "warning" | "outline" {
  switch (status) {
    case "InStock":
      return "success";
    case "Installed":
      return "warning";
    default:
      return "outline";
  }
}

export const CONSUMABLE_STOCK_DIRECTION_LABELS: Record<ConsumableStockDirection, string> = {
  In: "Entrada",
  Out: "Salida",
};

export const CONSUMABLE_STOCK_MOVEMENT_REASON_LABELS: Record<ConsumableStockMovementReason, string> = {
  InitialStock: "Existencia inicial",
  Purchase: "Compra",
  Consumption: "Consumo",
  Adjustment: "Ajuste",
};
