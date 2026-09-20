"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, requestAssetDecommission } from "@/lib/api";

export type RequestDecommissionActionState = { error: string | null };

export async function requestDecommissionAction(
  assetId: string,
  _prevState: RequestDecommissionActionState,
  formData: FormData,
): Promise<RequestDecommissionActionState> {
  const accessToken = await requireAccessToken();
  const justification = String(formData.get("justification") ?? "").trim();

  if (!justification) {
    return { error: "La justificación es obligatoria." };
  }

  try {
    // Redirects to the asset itself, not the approval instance: the requester may not hold
    // Approvals.Read (that's an admin permission) — seeing the asset now in "Pendiente de baja" is
    // confirmation enough that the request went through.
    await requestAssetDecommission(accessToken, assetId, justification);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible solicitar la baja." };
    }
    throw error;
  }

  redirect(`/assets/${assetId}`);
}
