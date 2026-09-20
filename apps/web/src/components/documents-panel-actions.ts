"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, uploadDocument } from "@/lib/api";

export type UploadDocumentActionState = { error: string | null };

export async function uploadDocumentAction(
  entityType: string,
  entityId: string,
  revalidatePathTarget: string,
  _prevState: UploadDocumentActionState,
  formData: FormData,
): Promise<UploadDocumentActionState> {
  const accessToken = await requireAccessToken();
  const file = formData.get("file") as File | null;

  if (!file || file.size === 0) {
    return { error: "Selecciona un archivo." };
  }

  try {
    await uploadDocument(accessToken, entityType, entityId, file);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible cargar el documento." };
    }
    throw error;
  }

  revalidatePath(revalidatePathTarget);
  return { error: null };
}
