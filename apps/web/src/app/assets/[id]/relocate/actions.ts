"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, relocateAsset } from "@/lib/api";

export type RelocateActionState = { error: string | null };

function emptyToNull(value: FormDataEntryValue | null): string | null {
  const text = typeof value === "string" ? value.trim() : "";
  return text === "" ? null : text;
}

export async function relocateAssetAction(
  assetId: string,
  _prevState: RelocateActionState,
  formData: FormData,
): Promise<RelocateActionState> {
  const accessToken = await requireAccessToken();

  try {
    await relocateAsset(accessToken, assetId, emptyToNull(formData.get("newOrgUnitId")), emptyToNull(formData.get("notes")));
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible reubicar el activo." };
    }
    throw error;
  }

  revalidatePath(`/assets/${assetId}`);
  redirect(`/assets/${assetId}`);
}
