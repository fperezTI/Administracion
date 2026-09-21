"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, linkAssetAccessory } from "@/lib/api";

export type LinkAccessoryActionState = { error: string | null };

export async function linkAccessoryAction(
  primaryAssetId: string,
  _prevState: LinkAccessoryActionState,
  formData: FormData,
): Promise<LinkAccessoryActionState> {
  const accessToken = await requireAccessToken();

  try {
    await linkAssetAccessory(accessToken, primaryAssetId, String(formData.get("accessoryAssetId") ?? ""));
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible vincular el accesorio." };
    }
    throw error;
  }

  revalidatePath(`/assets/${primaryAssetId}`);
  redirect(`/assets/${primaryAssetId}`);
}
