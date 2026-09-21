"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createAssignment } from "@/lib/api";

export type CreateAssignmentActionState = { error: string | null };

function emptyToNull(value: FormDataEntryValue | null): string | null {
  const text = typeof value === "string" ? value.trim() : "";
  return text === "" ? null : text;
}

export async function createAssignmentAction(
  _prevState: CreateAssignmentActionState,
  formData: FormData,
): Promise<CreateAssignmentActionState> {
  const accessToken = await requireAccessToken();

  const assetId = String(formData.get("assetId") ?? "");
  const assignedToUserId = String(formData.get("assignedToUserId") ?? "");

  if (!assetId || !assignedToUserId) {
    return { error: "Selecciona el activo y el destinatario." };
  }

  let assignmentId: string;
  try {
    const result = await createAssignment(accessToken, {
      assetId,
      assignedToUserId,
      orgUnitId: emptyToNull(formData.get("orgUnitId")),
      notes: emptyToNull(formData.get("notes")),
      accessoryAssetIds: formData.getAll("accessoryAssetIds").map(String),
    });
    assignmentId = result.assignmentId;
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible crear la asignación." };
    }
    throw error;
  }

  redirect(`/assignments/${assignmentId}`);
}
