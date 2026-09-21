"use server";

import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createUserFromDirectory } from "@/lib/api";

export type CreateUserFromDirectoryActionState = { error: string | null };

export async function createUserFromDirectoryAction(
  _prevState: CreateUserFromDirectoryActionState,
  formData: FormData,
): Promise<CreateUserFromDirectoryActionState> {
  const accessToken = await requireAccessToken();

  const roleId = String(formData.get("roleId") ?? "");
  if (!roleId) {
    return { error: "Selecciona un rol." };
  }

  let userId: string;
  try {
    userId = await createUserFromDirectory(accessToken, {
      entraObjectId: String(formData.get("entraObjectId") ?? ""),
      displayName: String(formData.get("displayName") ?? ""),
      email: String(formData.get("email") ?? ""),
      roleId,
      companyIds: formData.getAll("companyIds").map(String),
    });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible agregar al usuario." };
    }
    throw error;
  }

  revalidatePath("/users");
  redirect(`/users/${userId}`);
}
