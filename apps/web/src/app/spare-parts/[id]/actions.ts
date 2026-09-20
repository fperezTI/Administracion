"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, disposeSparePart, installSparePart, uninstallSparePart } from "@/lib/api";

export type SparePartActionState = { error: string | null };

export async function installSparePartAction(
  sparePartId: string,
  _prevState: SparePartActionState,
  formData: FormData,
): Promise<SparePartActionState> {
  const accessToken = await requireAccessToken();
  const assetId = String(formData.get("assetId") ?? "").trim();

  if (!assetId) {
    return { error: "Selecciona un activo." };
  }

  try {
    await installSparePart(accessToken, sparePartId, { assetId, maintenanceOrderId: null });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible instalar la refacción." };
    }
    throw error;
  }

  revalidatePath(`/spare-parts/${sparePartId}`);
  return { error: null };
}

export async function uninstallSparePartAction(
  sparePartId: string,
  _prevState: SparePartActionState,
  formData: FormData,
): Promise<SparePartActionState> {
  const accessToken = await requireAccessToken();
  const warehouseOrgUnitId = String(formData.get("warehouseOrgUnitId") ?? "").trim();

  if (!warehouseOrgUnitId) {
    return { error: "Selecciona un almacén." };
  }

  try {
    await uninstallSparePart(accessToken, sparePartId, warehouseOrgUnitId);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible retirar la refacción." };
    }
    throw error;
  }

  revalidatePath(`/spare-parts/${sparePartId}`);
  return { error: null };
}

export async function disposeSparePartAction(sparePartId: string, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await disposeSparePart(accessToken, sparePartId);
  revalidatePath(`/spare-parts/${sparePartId}`);
  revalidatePath("/spare-parts");
}
