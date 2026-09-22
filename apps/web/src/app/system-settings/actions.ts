"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, updateSystemSettings } from "@/lib/api";

export type SystemSettingsFormState = { error: string | null; success: boolean };

export async function updateSystemSettingsAction(
  _prevState: SystemSettingsFormState,
  formData: FormData,
): Promise<SystemSettingsFormState> {
  const accessToken = await requireAccessToken();

  const senderMailbox = String(formData.get("senderMailbox") ?? "").trim();
  const graphTenantId = String(formData.get("graphTenantId") ?? "").trim();
  const graphClientId = String(formData.get("graphClientId") ?? "").trim();
  const graphClientSecret = String(formData.get("graphClientSecret") ?? "").trim();

  try {
    await updateSystemSettings(accessToken, {
      senderMailbox: senderMailbox || null,
      graphTenantId: graphTenantId || null,
      graphClientId: graphClientId || null,
      graphClientSecret: graphClientSecret || null,
    });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible guardar los cambios.", success: false };
    }
    throw error;
  }

  revalidatePath("/system-settings");
  return { error: null, success: true };
}
