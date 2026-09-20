"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createRole } from "@/lib/api";

export type CreateRoleActionState = { error: string | null };

export async function createRoleAction(
  _prevState: CreateRoleActionState,
  formData: FormData,
): Promise<CreateRoleActionState> {
  const accessToken = await requireAccessToken();

  const name = String(formData.get("name") ?? "").trim();
  const descriptionRaw = String(formData.get("description") ?? "").trim();

  if (!name) {
    return { error: "El nombre es obligatorio." };
  }

  let roleId: string;
  try {
    roleId = await createRole(accessToken, { name, description: descriptionRaw || null });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible crear el rol." };
    }
    throw error;
  }

  redirect(`/roles/${roleId}`);
}
