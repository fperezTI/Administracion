"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, updateWarranty, type WarrantyType } from "@/lib/api";

export type EditWarrantyActionState = { error: string | null };

export async function editWarrantyAction(
  warrantyId: string,
  _prevState: EditWarrantyActionState,
  formData: FormData,
): Promise<EditWarrantyActionState> {
  const accessToken = await requireAccessToken();

  const companyId = String(formData.get("companyId") ?? "").trim();
  const type = String(formData.get("type") ?? "").trim() as WarrantyType;
  const provider = String(formData.get("provider") ?? "").trim();
  const startDate = String(formData.get("startDate") ?? "").trim();
  const endDate = String(formData.get("endDate") ?? "").trim();
  const terms = String(formData.get("terms") ?? "").trim();

  if (!type || !provider || !startDate || !endDate) {
    return { error: "Tipo, proveedor y fechas son obligatorios." };
  }

  try {
    await updateWarranty(accessToken, warrantyId, { type, provider, startDate, endDate, terms: terms || null });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible actualizar la garantía." };
    }
    throw error;
  }

  redirect(`/warranties?companyId=${companyId}`);
}
