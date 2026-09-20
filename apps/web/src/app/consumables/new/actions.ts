"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createConsumable } from "@/lib/api";

export type CreateConsumableActionState = { error: string | null };

export async function createConsumableAction(
  _prevState: CreateConsumableActionState,
  formData: FormData,
): Promise<CreateConsumableActionState> {
  const accessToken = await requireAccessToken();

  const companyId = String(formData.get("companyId") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const sku = String(formData.get("sku") ?? "").trim();
  const unitOfMeasure = String(formData.get("unitOfMeasure") ?? "").trim();
  const minimumStockRaw = String(formData.get("minimumStock") ?? "").trim();
  const minimumStock = minimumStockRaw ? Number(minimumStockRaw) : null;

  if (!companyId || !name || !unitOfMeasure) {
    return { error: "Nombre y unidad de medida son obligatorios." };
  }

  let consumableId: string;
  try {
    consumableId = await createConsumable(accessToken, { companyId, name, sku: sku || null, unitOfMeasure, minimumStock });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible registrar el consumible." };
    }
    throw error;
  }

  redirect(`/consumables/${consumableId}?companyId=${companyId}`);
}
