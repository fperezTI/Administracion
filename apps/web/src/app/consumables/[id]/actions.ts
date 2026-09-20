"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, registerConsumableStockMovement, type ConsumableStockDirection, type ConsumableStockMovementReason } from "@/lib/api";

export type RegisterMovementActionState = { error: string | null };

export async function registerMovementAction(
  consumableId: string,
  _prevState: RegisterMovementActionState,
  formData: FormData,
): Promise<RegisterMovementActionState> {
  const accessToken = await requireAccessToken();

  const warehouseOrgUnitId = String(formData.get("warehouseOrgUnitId") ?? "").trim();
  const direction = String(formData.get("direction") ?? "").trim() as ConsumableStockDirection;
  const reason = String(formData.get("reason") ?? "").trim() as ConsumableStockMovementReason;
  const quantityRaw = String(formData.get("quantity") ?? "").trim();
  const quantity = Number(quantityRaw);
  const notes = String(formData.get("notes") ?? "").trim();

  if (!warehouseOrgUnitId || !direction || !reason || !quantityRaw || Number.isNaN(quantity) || quantity <= 0) {
    return { error: "Almacén, dirección, motivo y una cantidad mayor a cero son obligatorios." };
  }

  try {
    await registerConsumableStockMovement(accessToken, consumableId, {
      warehouseOrgUnitId,
      direction,
      reason,
      quantity,
      referenceMaintenanceOrderId: null,
      notes: notes || null,
    });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible registrar el movimiento." };
    }
    throw error;
  }

  revalidatePath(`/consumables/${consumableId}`);
  revalidatePath("/consumables");
  return { error: null };
}
