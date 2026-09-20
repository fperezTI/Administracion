"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, cancelTransfer, receiveTransfer, type SignatureMechanism } from "@/lib/api";

export type TransferActionState = { error: string | null; success: boolean };

export async function cancelTransferAction(transferId: string, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await cancelTransfer(accessToken, transferId);
  revalidatePath(`/transfers/${transferId}`);
  revalidatePath("/transfers");
}

export async function receiveTransferAction(
  transferId: string,
  _prevState: TransferActionState,
  formData: FormData,
): Promise<TransferActionState> {
  const accessToken = await requireAccessToken();

  const input = {
    comment: null,
    signatureMechanism: String(formData.get("signatureMechanism") ?? "TypedConfirmation") as SignatureMechanism,
    typedFullName: (String(formData.get("typedFullName") ?? "").trim() || null),
    signatureImageDataUrl: (String(formData.get("signatureImageDataUrl") ?? "").trim() || null),
  };

  try {
    await receiveTransfer(accessToken, transferId, input);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible recibir la transferencia.", success: false };
    }
    throw error;
  }

  revalidatePath(`/transfers/${transferId}`);
  revalidatePath("/transfers");
  return { error: null, success: true };
}
