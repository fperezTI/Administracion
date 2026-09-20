"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, addTemplateVersion, setTemplateActive } from "@/lib/api";

export type AddVersionActionState = { error: string | null; success: boolean };

export async function addVersionAction(
  templateId: string,
  _prevState: AddVersionActionState,
  formData: FormData,
): Promise<AddVersionActionState> {
  const accessToken = await requireAccessToken();
  const content = String(formData.get("content") ?? "").trim();

  if (!content) {
    return { error: "El contenido es obligatorio.", success: false };
  }

  try {
    await addTemplateVersion(accessToken, templateId, content);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible agregar la versión.", success: false };
    }
    throw error;
  }

  revalidatePath(`/templates/${templateId}`);
  return { error: null, success: true };
}

export async function toggleTemplateActiveAction(templateId: string, isActive: boolean, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await setTemplateActive(accessToken, templateId, isActive);
  revalidatePath(`/templates/${templateId}`);
  revalidatePath("/templates");
}
