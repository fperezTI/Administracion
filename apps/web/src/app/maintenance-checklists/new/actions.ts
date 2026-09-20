"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createMaintenanceChecklist } from "@/lib/api";

export type CreateChecklistActionState = { error: string | null };

export async function createChecklistAction(
  _prevState: CreateChecklistActionState,
  formData: FormData,
): Promise<CreateChecklistActionState> {
  const accessToken = await requireAccessToken();

  const key = String(formData.get("key") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const assetCategoryId = String(formData.get("assetCategoryId") ?? "").trim();
  const itemsRaw = String(formData.get("items") ?? "");
  const initialItems = itemsRaw.split("\n").map((line) => line.trim()).filter((line) => line.length > 0);

  if (!key || !name || initialItems.length === 0) {
    return { error: "Clave, nombre y al menos un ítem son obligatorios." };
  }

  let checklistId: string;
  try {
    checklistId = await createMaintenanceChecklist(accessToken, {
      key,
      name,
      assetCategoryId: assetCategoryId || null,
      initialItems,
    });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible crear el checklist." };
    }
    throw error;
  }

  redirect(`/maintenance-checklists/${checklistId}`);
}
