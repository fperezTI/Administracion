"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createLoan } from "@/lib/api";

export type CreateLoanActionState = { error: string | null };

function emptyToNull(value: FormDataEntryValue | null): string | null {
  const text = typeof value === "string" ? value.trim() : "";
  return text === "" ? null : text;
}

export async function createLoanAction(
  _prevState: CreateLoanActionState,
  formData: FormData,
): Promise<CreateLoanActionState> {
  const accessToken = await requireAccessToken();

  const assetId = String(formData.get("assetId") ?? "");
  const borrowerUserId = String(formData.get("borrowerUserId") ?? "");
  const expectedReturnDate = String(formData.get("expectedReturnDate") ?? "");

  if (!assetId || !borrowerUserId || !expectedReturnDate) {
    return { error: "Selecciona el activo, el destinatario y la fecha esperada de devolución." };
  }

  let loanId: string;
  try {
    const result = await createLoan(accessToken, {
      assetId,
      borrowerUserId,
      expectedReturnDate,
      notes: emptyToNull(formData.get("notes")),
    });
    loanId = result.loanId;
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible crear el préstamo." };
    }
    throw error;
  }

  redirect(`/loans/${loanId}`);
}
