"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, requestAssetDisposal } from "@/lib/api";

export type RequestDisposalActionState = { error: string | null };

export async function requestDisposalAction(
  assetId: string,
  _prevState: RequestDisposalActionState,
  formData: FormData,
): Promise<RequestDisposalActionState> {
  const accessToken = await requireAccessToken();
  const targetStatus = String(formData.get("targetStatus") ?? "") as "Sold" | "Donated" | "Destroyed";
  const justification = String(formData.get("justification") ?? "").trim();

  if (!justification) {
    return { error: "La justificación es obligatoria." };
  }

  try {
    await requestAssetDisposal(accessToken, assetId, targetStatus, justification);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible solicitar la disposición." };
    }
    throw error;
  }

  redirect(`/assets/${assetId}`);
}
