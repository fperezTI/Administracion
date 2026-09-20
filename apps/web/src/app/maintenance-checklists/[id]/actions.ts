"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, addMaintenanceChecklistVersion, setMaintenanceChecklistActive } from "@/lib/api";

export type AddChecklistVersionActionState = { error: string | null; success: boolean };

export async function addChecklistVersionAction(
  checklistDefinitionId: string,
  _prevState: AddChecklistVersionActionState,
  formData: FormData,
): Promise<AddChecklistVersionActionState> {
  const accessToken = await requireAccessToken();
  const itemsRaw = String(formData.get("items") ?? "");
  const items = itemsRaw.split("\n").map((line) => line.trim()).filter((line) => line.length > 0);

  if (items.length === 0) {
    return { error: "Debes indicar al menos un ítem.", success: false };
  }

  try {
    await addMaintenanceChecklistVersion(accessToken, checklistDefinitionId, items);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible agregar la versión.", success: false };
    }
    throw error;
  }

  revalidatePath(`/maintenance-checklists/${checklistDefinitionId}`);
  return { error: null, success: true };
}

export async function toggleChecklistActiveAction(checklistDefinitionId: string, isActive: boolean, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await setMaintenanceChecklistActive(accessToken, checklistDefinitionId, isActive);
  revalidatePath(`/maintenance-checklists/${checklistDefinitionId}`);
  revalidatePath("/maintenance-checklists");
}
