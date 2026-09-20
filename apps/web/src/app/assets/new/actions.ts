"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createAsset, type CreateAssetInput, type PhysicalCondition } from "@/lib/api";

export type CreateAssetActionState = {
  error: string | null;
};

const CUSTOM_FIELD_PREFIX = "customField:";

function emptyToNull(value: FormDataEntryValue | null): string | null {
  const text = typeof value === "string" ? value.trim() : "";
  return text === "" ? null : text;
}

export async function createAssetAction(
  _prevState: CreateAssetActionState,
  formData: FormData,
): Promise<CreateAssetActionState> {
  const accessToken = await requireAccessToken();

  const customFieldValues: Record<string, string> = {};
  for (const [key, value] of formData.entries()) {
    if (key.startsWith(CUSTOM_FIELD_PREFIX) && typeof value === "string" && value.trim() !== "") {
      customFieldValues[key.slice(CUSTOM_FIELD_PREFIX.length)] = value.trim();
    }
  }

  const companyId = String(formData.get("companyId") ?? "");
  const assetCategoryId = String(formData.get("assetCategoryId") ?? "");
  const brand = String(formData.get("brand") ?? "").trim();
  const model = String(formData.get("model") ?? "").trim();

  if (!companyId || !assetCategoryId || !brand || !model) {
    return { error: "Empresa, categoría, marca y modelo son obligatorios." };
  }

  const input: CreateAssetInput = {
    companyId,
    assetCategoryId,
    brand,
    model,
    serialNumber: emptyToNull(formData.get("serialNumber")),
    description: emptyToNull(formData.get("description")),
    physicalCondition: String(formData.get("physicalCondition") ?? "Good") as PhysicalCondition,
    currentOrgUnitId: emptyToNull(formData.get("currentOrgUnitId")),
    customFieldValues: Object.keys(customFieldValues).length > 0 ? customFieldValues : null,
  };

  let assetId: string;
  try {
    const result = await createAsset(accessToken, input);
    assetId = result.assetId;
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible crear el activo." };
    }
    throw error;
  }

  redirect(`/assets/${assetId}`);
}
