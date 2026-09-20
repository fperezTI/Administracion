"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import {
  ApiError,
  updateAssetContractualInfo,
  updateAssetFinancialInfo,
  updateAssetGeneralInfo,
  type PhysicalCondition,
} from "@/lib/api";

export type EditActionState = {
  error: string | null;
  success: boolean;
};

function emptyToNull(value: FormDataEntryValue | null): string | null {
  const text = typeof value === "string" ? value.trim() : "";
  return text === "" ? null : text;
}

const CUSTOM_FIELD_PREFIX = "customField:";

export async function updateGeneralAction(
  _prevState: EditActionState,
  formData: FormData,
): Promise<EditActionState> {
  const accessToken = await requireAccessToken();
  const assetId = String(formData.get("assetId") ?? "");

  const customFieldValues: Record<string, string> = {};
  for (const [key, value] of formData.entries()) {
    if (key.startsWith(CUSTOM_FIELD_PREFIX) && typeof value === "string" && value.trim() !== "") {
      customFieldValues[key.slice(CUSTOM_FIELD_PREFIX.length)] = value.trim();
    }
  }

  try {
    await updateAssetGeneralInfo(accessToken, assetId, {
      brand: String(formData.get("brand") ?? "").trim(),
      model: String(formData.get("model") ?? "").trim(),
      serialNumber: emptyToNull(formData.get("serialNumber")),
      description: emptyToNull(formData.get("description")),
      patrimonialFolio: emptyToNull(formData.get("patrimonialFolio")),
      physicalCondition: String(formData.get("physicalCondition") ?? "Good") as PhysicalCondition,
      customFieldValues: Object.keys(customFieldValues).length > 0 ? customFieldValues : null,
    });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible guardar los cambios.", success: false };
    }
    throw error;
  }

  revalidatePath(`/assets/${assetId}`);
  return { error: null, success: true };
}

export async function updateFinancialAction(
  _prevState: EditActionState,
  formData: FormData,
): Promise<EditActionState> {
  const accessToken = await requireAccessToken();
  const assetId = String(formData.get("assetId") ?? "");
  const cost = emptyToNull(formData.get("acquisitionCost"));

  try {
    await updateAssetFinancialInfo(accessToken, assetId, {
      acquisitionDate: emptyToNull(formData.get("acquisitionDate")),
      acquisitionCost: cost !== null ? Number(cost) : null,
      currency: emptyToNull(formData.get("currency")),
      supplier: emptyToNull(formData.get("supplier")),
      invoice: emptyToNull(formData.get("invoice")),
      purchaseOrder: emptyToNull(formData.get("purchaseOrder")),
    });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible guardar los cambios.", success: false };
    }
    throw error;
  }

  revalidatePath(`/assets/${assetId}`);
  return { error: null, success: true };
}

export async function updateContractualAction(
  _prevState: EditActionState,
  formData: FormData,
): Promise<EditActionState> {
  const accessToken = await requireAccessToken();
  const assetId = String(formData.get("assetId") ?? "");

  try {
    await updateAssetContractualInfo(accessToken, assetId, {
      warrantyStartDate: emptyToNull(formData.get("warrantyStartDate")),
      warrantyEndDate: emptyToNull(formData.get("warrantyEndDate")),
      supportContract: emptyToNull(formData.get("supportContract")),
      supportProvider: emptyToNull(formData.get("supportProvider")),
    });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible guardar los cambios.", success: false };
    }
    throw error;
  }

  revalidatePath(`/assets/${assetId}`);
  return { error: null, success: true };
}
