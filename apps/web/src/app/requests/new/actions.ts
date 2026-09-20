"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createInternalRequest, type InternalRequestType } from "@/lib/api";

export type CreateRequestActionState = { error: string | null };

export async function createRequestAction(
  _prevState: CreateRequestActionState,
  formData: FormData,
): Promise<CreateRequestActionState> {
  const accessToken = await requireAccessToken();

  const type = String(formData.get("type") ?? "").trim() as InternalRequestType;
  const assetId = String(formData.get("assetId") ?? "").trim();
  const justification = String(formData.get("justification") ?? "").trim();
  const expectedReturnDate = String(formData.get("expectedReturnDate") ?? "").trim();

  if (!type || !assetId || !justification) {
    return { error: "Tipo, activo y justificación son obligatorios." };
  }

  if (type === "Loan" && !expectedReturnDate) {
    return { error: "Una solicitud de préstamo requiere la fecha esperada de devolución." };
  }

  try {
    await createInternalRequest(accessToken, {
      type,
      assetId,
      justification,
      expectedReturnDate: type === "Loan" ? expectedReturnDate : null,
    });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible enviar la solicitud." };
    }
    throw error;
  }

  redirect("/my-requests");
}
