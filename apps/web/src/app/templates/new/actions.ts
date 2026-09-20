"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createTemplate } from "@/lib/api";

export type CreateTemplateActionState = { error: string | null };

export async function createTemplateAction(
  _prevState: CreateTemplateActionState,
  formData: FormData,
): Promise<CreateTemplateActionState> {
  const accessToken = await requireAccessToken();

  const key = String(formData.get("key") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const initialContent = String(formData.get("initialContent") ?? "").trim();

  if (!key || !name || !initialContent) {
    return { error: "Clave, nombre y contenido son obligatorios." };
  }

  let templateId: string;
  try {
    templateId = await createTemplate(accessToken, { key, name, initialContent });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible crear la plantilla." };
    }
    throw error;
  }

  redirect(`/templates/${templateId}`);
}
