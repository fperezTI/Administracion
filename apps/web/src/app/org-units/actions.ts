"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createOrgUnit, moveOrgUnit, setOrgUnitActive } from "@/lib/api";

export type CreateOrgUnitActionState = { error: string | null };

export async function createOrgUnitAction(
  companyId: string,
  _prevState: CreateOrgUnitActionState,
  formData: FormData,
): Promise<CreateOrgUnitActionState> {
  const accessToken = await requireAccessToken();

  const name = String(formData.get("name") ?? "").trim();
  const code = String(formData.get("code") ?? "").trim();
  const orgUnitTypeId = String(formData.get("orgUnitTypeId") ?? "");
  const parentRaw = String(formData.get("parentOrgUnitId") ?? "");

  if (!name || !code || !orgUnitTypeId) {
    return { error: "Nombre, código y tipo son obligatorios." };
  }

  try {
    await createOrgUnit(accessToken, {
      companyId,
      orgUnitTypeId,
      parentOrgUnitId: parentRaw || null,
      name,
      code,
    });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible crear la unidad organizacional." };
    }
    throw error;
  }

  revalidatePath("/org-units");
  return { error: null };
}

export async function moveOrgUnitAction(orgUnitId: string, formData: FormData) {
  const accessToken = await requireAccessToken();
  const newParentRaw = String(formData.get(`newParent:${orgUnitId}`) ?? "");
  await moveOrgUnit(accessToken, orgUnitId, newParentRaw || null);
  revalidatePath("/org-units");
}

export async function toggleOrgUnitActiveAction(orgUnitId: string, isActive: boolean, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await setOrgUnitActive(accessToken, orgUnitId, isActive);
  revalidatePath("/org-units");
}
