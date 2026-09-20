"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, cancelImportBatch, commitImportBatch, type ImportCommitMode } from "@/lib/api";

export type CommitImportBatchActionState = { error: string | null };

export async function commitImportBatchAction(
  importBatchId: string,
  _prevState: CommitImportBatchActionState,
  formData: FormData,
): Promise<CommitImportBatchActionState> {
  const accessToken = await requireAccessToken();
  const mode = String(formData.get("mode") ?? "") as ImportCommitMode;

  if (mode !== "AllOrNothing" && mode !== "ValidRowsOnly") {
    return { error: "Elige un modo de confirmación." };
  }

  try {
    await commitImportBatch(accessToken, importBatchId, mode);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible confirmar la importación." };
    }
    throw error;
  }

  revalidatePath(`/imports/${importBatchId}`);
  return { error: null };
}

export async function cancelImportBatchAction(importBatchId: string, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await cancelImportBatch(accessToken, importBatchId);
  revalidatePath(`/imports/${importBatchId}`);
  revalidatePath("/imports");
}
