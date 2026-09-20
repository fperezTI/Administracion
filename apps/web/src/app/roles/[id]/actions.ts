"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, duplicateRole, setRoleActive, setRolePermissions, updateRole } from "@/lib/api";

export type RoleFormState = { error: string | null; success: boolean };

export async function updateRoleAction(
  roleId: string,
  _prevState: RoleFormState,
  formData: FormData,
): Promise<RoleFormState> {
  const accessToken = await requireAccessToken();
  const name = String(formData.get("name") ?? "").trim();
  const descriptionRaw = String(formData.get("description") ?? "").trim();

  if (!name) {
    return { error: "El nombre es obligatorio.", success: false };
  }

  try {
    await updateRole(accessToken, roleId, { name, description: descriptionRaw || null });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible guardar los cambios.", success: false };
    }
    throw error;
  }

  revalidatePath(`/roles/${roleId}`);
  revalidatePath("/roles");
  return { error: null, success: true };
}

export async function savePermissionsAction(
  roleId: string,
  _prevState: RoleFormState,
  formData: FormData,
): Promise<RoleFormState> {
  const accessToken = await requireAccessToken();
  const permissionIds = formData.getAll("permissionId").map(String);

  try {
    await setRolePermissions(accessToken, roleId, permissionIds);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible guardar los permisos.", success: false };
    }
    throw error;
  }

  revalidatePath(`/roles/${roleId}`);
  revalidatePath("/roles");
  return { error: null, success: true };
}

export async function toggleRoleActiveAction(roleId: string, isActive: boolean, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await setRoleActive(accessToken, roleId, isActive);
  revalidatePath(`/roles/${roleId}`);
  revalidatePath("/roles");
}

export type DuplicateRoleActionState = { error: string | null };

export async function duplicateRoleAction(
  sourceRoleId: string,
  _prevState: DuplicateRoleActionState,
  formData: FormData,
): Promise<DuplicateRoleActionState> {
  const accessToken = await requireAccessToken();
  const newName = String(formData.get("newName") ?? "").trim();

  if (!newName) {
    return { error: "Escribe un nombre para el rol duplicado." };
  }

  let newRoleId: string;
  try {
    newRoleId = await duplicateRole(accessToken, sourceRoleId, newName);
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible duplicar el rol." };
    }
    throw error;
  }

  redirect(`/roles/${newRoleId}`);
}
