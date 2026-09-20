"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createSparePart } from "@/lib/api";

export type CreateSparePartActionState = { error: string | null };

export async function createSparePartAction(
  _prevState: CreateSparePartActionState,
  formData: FormData,
): Promise<CreateSparePartActionState> {
  const accessToken = await requireAccessToken();

  const companyId = String(formData.get("companyId") ?? "").trim();
  const name = String(formData.get("name") ?? "").trim();
  const partNumber = String(formData.get("partNumber") ?? "").trim();
  const serialNumber = String(formData.get("serialNumber") ?? "").trim();
  const warehouseOrgUnitId = String(formData.get("warehouseOrgUnitId") ?? "").trim();

  if (!companyId || !name || !serialNumber || !warehouseOrgUnitId) {
    return { error: "Nombre, número de serie y almacén son obligatorios." };
  }

  let sparePartId: string;
  try {
    sparePartId = await createSparePart(accessToken, {
      companyId,
      name,
      partNumber: partNumber || null,
      serialNumber,
      warehouseOrgUnitId,
    });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible registrar la refacción." };
    }
    throw error;
  }

  redirect(`/spare-parts/${sparePartId}?companyId=${companyId}`);
}
