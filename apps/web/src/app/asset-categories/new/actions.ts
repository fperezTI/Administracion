"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createAssetCategory, type IdentificationTechnology } from "@/lib/api";

export type CreateCategoryActionState = { error: string | null };

export async function createCategoryAction(
  _prevState: CreateCategoryActionState,
  formData: FormData,
): Promise<CreateCategoryActionState> {
  const accessToken = await requireAccessToken();

  const name = String(formData.get("name") ?? "").trim();
  const code = String(formData.get("code") ?? "").trim();
  const defaultIdentificationTechnology = String(
    formData.get("defaultIdentificationTechnology") ?? "Qr",
  ) as IdentificationTechnology;

  if (!name || !code) {
    return { error: "Nombre y código son obligatorios." };
  }

  let categoryId: string;
  try {
    categoryId = await createAssetCategory(accessToken, { name, code, defaultIdentificationTechnology });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible crear la categoría." };
    }
    throw error;
  }

  redirect(`/asset-categories/${categoryId}`);
}
