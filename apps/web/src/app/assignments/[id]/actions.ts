"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, cancelAssignment, returnAssignment } from "@/lib/api";

export type AssignmentActionState = { error: string | null; success: boolean };

export async function cancelAssignmentAction(assignmentId: string, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await cancelAssignment(accessToken, assignmentId);
  revalidatePath(`/assignments/${assignmentId}`);
  revalidatePath("/assignments");
}

export async function returnAssignmentAction(
  assignmentId: string,
  _prevState: AssignmentActionState,
  formData: FormData,
): Promise<AssignmentActionState> {
  const accessToken = await requireAccessToken();
  const typedFullName = String(formData.get("typedFullName") ?? "").trim();
  const notesRaw = String(formData.get("notes") ?? "").trim();

  if (!typedFullName) {
    return { error: "Escribe tu nombre completo para confirmar.", success: false };
  }

  try {
    await returnAssignment(accessToken, assignmentId, typedFullName, notesRaw || null);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible registrar la devolución.", success: false };
    }
    throw error;
  }

  revalidatePath(`/assignments/${assignmentId}`);
  revalidatePath("/assignments");
  return { error: null, success: true };
}
