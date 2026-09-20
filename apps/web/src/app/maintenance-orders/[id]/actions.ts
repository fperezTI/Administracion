"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, closeMaintenanceOrder, type AssetStatus } from "@/lib/api";

export type CloseMaintenanceOrderActionState = { error: string | null };

export async function closeMaintenanceOrderAction(
  maintenanceOrderId: string,
  _prevState: CloseMaintenanceOrderActionState,
  formData: FormData,
): Promise<CloseMaintenanceOrderActionState> {
  const accessToken = await requireAccessToken();

  const resultStatus = String(formData.get("resultStatus") ?? "").trim() as AssetStatus;
  const resultNotes = String(formData.get("resultNotes") ?? "").trim();
  const itemIndexes = String(formData.get("itemIndexes") ?? "")
    .split(",")
    .map((s) => s.trim())
    .filter((s) => s.length > 0)
    .map(Number);

  if (!resultStatus || !resultNotes) {
    return { error: "El resultado y la descripción son obligatorios." };
  }

  const checklistItemResults =
    itemIndexes.length > 0
      ? itemIndexes.map((itemIndex) => ({
          itemIndex,
          isCompleted: formData.get(`item-${itemIndex}-completed`) === "on",
          notes: String(formData.get(`item-${itemIndex}-notes`) ?? "").trim() || null,
        }))
      : null;

  try {
    await closeMaintenanceOrder(accessToken, maintenanceOrderId, { resultStatus, resultNotes, checklistItemResults });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible cerrar la orden de mantenimiento." };
    }
    throw error;
  }

  revalidatePath(`/maintenance-orders/${maintenanceOrderId}`);
  revalidatePath("/maintenance-orders");
  return { error: null };
}
