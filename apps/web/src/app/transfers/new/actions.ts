"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, requestTransfer } from "@/lib/api";

export type RequestTransferActionState = { error: string | null };

function emptyToNull(value: FormDataEntryValue | null): string | null {
  const text = typeof value === "string" ? value.trim() : "";
  return text === "" ? null : text;
}

export async function requestTransferAction(
  _prevState: RequestTransferActionState,
  formData: FormData,
): Promise<RequestTransferActionState> {
  const accessToken = await requireAccessToken();

  const assetId = String(formData.get("assetId") ?? "");
  const toCompanyId = String(formData.get("toCompanyId") ?? "");

  if (!assetId || !toCompanyId) {
    return { error: "Selecciona el activo y la empresa destino." };
  }

  let transferId: string;
  try {
    transferId = await requestTransfer(accessToken, { assetId, toCompanyId, notes: emptyToNull(formData.get("notes")) });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible solicitar la transferencia." };
    }
    throw error;
  }

  redirect(`/transfers/${transferId}`);
}
