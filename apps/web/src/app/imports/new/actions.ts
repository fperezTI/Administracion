"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, uploadImportBatch } from "@/lib/api";

export type UploadImportBatchActionState = { error: string | null };

export async function uploadImportBatchAction(
  companyId: string,
  _prevState: UploadImportBatchActionState,
  formData: FormData,
): Promise<UploadImportBatchActionState> {
  const accessToken = await requireAccessToken();
  const file = formData.get("file") as File | null;

  if (!file || file.size === 0) {
    return { error: "Selecciona un archivo CSV." };
  }

  let batchId: string;
  try {
    batchId = await uploadImportBatch(accessToken, companyId, file);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible subir el archivo." };
    }
    throw error;
  }

  redirect(`/imports/${batchId}`);
}
