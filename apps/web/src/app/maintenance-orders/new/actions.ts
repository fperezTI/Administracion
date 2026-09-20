"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, openMaintenanceOrder, type MaintenanceOrderType } from "@/lib/api";

export type OpenMaintenanceOrderActionState = { error: string | null };

export async function openMaintenanceOrderAction(
  _prevState: OpenMaintenanceOrderActionState,
  formData: FormData,
): Promise<OpenMaintenanceOrderActionState> {
  const accessToken = await requireAccessToken();

  const assetId = String(formData.get("assetId") ?? "").trim();
  const type = String(formData.get("type") ?? "").trim() as MaintenanceOrderType;
  const description = String(formData.get("description") ?? "").trim();
  const checklistDefinitionId = String(formData.get("checklistDefinitionId") ?? "").trim();

  if (!assetId || !type || !description) {
    return { error: "Activo, tipo y descripción son obligatorios." };
  }

  let orderId: string;
  try {
    orderId = await openMaintenanceOrder(accessToken, {
      assetId,
      type,
      description,
      checklistDefinitionId: checklistDefinitionId || null,
    });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible abrir la orden de mantenimiento." };
    }
    throw error;
  }

  redirect(`/maintenance-orders/${orderId}`);
}
