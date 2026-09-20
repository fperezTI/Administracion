"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, addCustomFieldDefinition, setAssetCategoryActive, type CustomFieldDataType } from "@/lib/api";

export type AddFieldActionState = { error: string | null };

export async function addFieldAction(
  categoryId: string,
  _prevState: AddFieldActionState,
  formData: FormData,
): Promise<AddFieldActionState> {
  const accessToken = await requireAccessToken();

  const name = String(formData.get("name") ?? "").trim();
  const code = String(formData.get("code") ?? "").trim();
  const dataType = String(formData.get("dataType") ?? "Text") as CustomFieldDataType;
  const isRequired = formData.get("isRequired") === "on";
  const optionsRaw = String(formData.get("options") ?? "").trim();

  if (!name || !code) {
    return { error: "Nombre y código son obligatorios." };
  }
  if (dataType === "Select" && !optionsRaw) {
    return { error: "Un campo de selección necesita al menos una opción." };
  }

  try {
    await addCustomFieldDefinition(accessToken, categoryId, {
      name,
      code,
      dataType,
      isRequired,
      options: optionsRaw || null,
    });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible agregar el campo." };
    }
    throw error;
  }

  revalidatePath(`/asset-categories/${categoryId}`);
  return { error: null };
}

export async function toggleCategoryActiveAction(categoryId: string, isActive: boolean, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await setAssetCategoryActive(accessToken, categoryId, isActive);
  revalidatePath(`/asset-categories/${categoryId}`);
  revalidatePath("/asset-categories");
}
