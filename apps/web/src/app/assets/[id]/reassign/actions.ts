"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, reassignAsset } from "@/lib/api";

export type ReassignActionState = { error: string | null };

function emptyToNull(value: FormDataEntryValue | null): string | null {
  const text = typeof value === "string" ? value.trim() : "";
  return text === "" ? null : text;
}

export async function reassignAssetAction(
  assetId: string,
  _prevState: ReassignActionState,
  formData: FormData,
): Promise<ReassignActionState> {
  const accessToken = await requireAccessToken();

  try {
    await reassignAsset(accessToken, {
      assetId,
      newAssignedToUserId: String(formData.get("newAssignedToUserId") ?? ""),
      orgUnitId: emptyToNull(formData.get("orgUnitId")),
      typedFullName: String(formData.get("typedFullName") ?? "").trim(),
      notes: emptyToNull(formData.get("notes")),
      accessoryAssetIds: formData.getAll("accessoryAssetIds").map(String),
    });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible reasignar el activo." };
    }
    throw error;
  }

  revalidatePath(`/assets/${assetId}`);
  redirect(`/assets/${assetId}`);
}
