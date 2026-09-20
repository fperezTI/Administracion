"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, returnLoan } from "@/lib/api";

export type ReturnLoanActionState = { error: string | null; success: boolean };

export async function returnLoanAction(
  loanId: string,
  _prevState: ReturnLoanActionState,
  formData: FormData,
): Promise<ReturnLoanActionState> {
  const accessToken = await requireAccessToken();
  const notesRaw = String(formData.get("notes") ?? "").trim();

  try {
    await returnLoan(accessToken, loanId, notesRaw || null);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible registrar la devolución.", success: false };
    }
    throw error;
  }

  revalidatePath(`/loans/${loanId}`);
  revalidatePath("/loans");
  return { error: null, success: true };
}
