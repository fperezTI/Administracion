"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, signAssignment } from "@/lib/api";

export type SignActionState = { error: string | null; success: boolean };

export async function signAssignmentAction(
  assignmentId: string,
  _prevState: SignActionState,
  formData: FormData,
): Promise<SignActionState> {
  const accessToken = await requireAccessToken();
  const typedFullName = String(formData.get("typedFullName") ?? "").trim();

  if (!typedFullName) {
    return { error: "Escribe tu nombre completo para confirmar.", success: false };
  }

  try {
    await signAssignment(accessToken, assignmentId, typedFullName);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible confirmar la recepción.", success: false };
    }
    throw error;
  }

  revalidatePath("/my-assignments");
  return { error: null, success: true };
}
