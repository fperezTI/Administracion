"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createWarranty, type WarrantyType } from "@/lib/api";

export type CreateWarrantyActionState = { error: string | null };

export async function createWarrantyAction(
  _prevState: CreateWarrantyActionState,
  formData: FormData,
): Promise<CreateWarrantyActionState> {
  const accessToken = await requireAccessToken();

  const companyId = String(formData.get("companyId") ?? "").trim();
  const assetId = String(formData.get("assetId") ?? "").trim();
  const type = String(formData.get("type") ?? "").trim() as WarrantyType;
  const provider = String(formData.get("provider") ?? "").trim();
  const startDate = String(formData.get("startDate") ?? "").trim();
  const endDate = String(formData.get("endDate") ?? "").trim();
  const terms = String(formData.get("terms") ?? "").trim();

  if (!assetId || !type || !provider || !startDate || !endDate) {
    return { error: "Activo, tipo, proveedor y fechas son obligatorios." };
  }

  try {
    await createWarranty(accessToken, { assetId, type, provider, startDate, endDate, terms: terms || null });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible registrar la garantía." };
    }
    throw error;
  }

  redirect(`/warranties?companyId=${companyId}`);
}
