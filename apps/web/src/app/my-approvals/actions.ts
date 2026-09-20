"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, approveApprovalStep, rejectApprovalStep, type SignatureMechanism } from "@/lib/api";

export type DecideActionState = { error: string | null; success: boolean };

function readDecideInput(formData: FormData) {
  return {
    comment: (String(formData.get("comment") ?? "").trim() || null),
    signatureMechanism: String(formData.get("signatureMechanism") ?? "TypedConfirmation") as SignatureMechanism,
    typedFullName: (String(formData.get("typedFullName") ?? "").trim() || null),
    signatureImageDataUrl: (String(formData.get("signatureImageDataUrl") ?? "").trim() || null),
  };
}

export async function approveAction(
  approvalInstanceId: string,
  _prevState: DecideActionState,
  formData: FormData,
): Promise<DecideActionState> {
  const accessToken = await requireAccessToken();

  try {
    await approveApprovalStep(accessToken, approvalInstanceId, readDecideInput(formData));
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible aprobar.", success: false };
    }
    throw error;
  }

  revalidatePath("/my-approvals");
  return { error: null, success: true };
}

export async function rejectAction(
  approvalInstanceId: string,
  _prevState: DecideActionState,
  formData: FormData,
): Promise<DecideActionState> {
  const accessToken = await requireAccessToken();
  const input = readDecideInput(formData);

  if (!input.comment) {
    return { error: "Escribe el motivo del rechazo.", success: false };
  }

  try {
    await rejectApprovalStep(accessToken, approvalInstanceId, input);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible rechazar.", success: false };
    }
    throw error;
  }

  revalidatePath("/my-approvals");
  return { error: null, success: true };
}
