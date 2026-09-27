"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, setCompanyActive, setCompanyDefaultTheme } from "@/lib/api";
import { isThemeCode } from "@/lib/theme-catalog";

export async function toggleCompanyActiveAction(companyId: string, isActive: boolean, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await setCompanyActive(accessToken, companyId, isActive);
  revalidatePath("/companies");
}

export type SetCompanyThemeResult = { error: string } | null;

/** `SetCompanyDefaultThemeCommand` requires `Companies.Update` on the backend — a user without it
 * gets a 403 here, surfaced as an inline error instead of a broken redirect (pedido: RBAC se valida
 * en backend, nunca solo en la UI; esta acción no decide permisos, solo traduce el resultado). */
export async function setCompanyThemeAction(companyId: string, themeCode: string): Promise<SetCompanyThemeResult> {
  if (!isThemeCode(themeCode)) {
    return { error: "Tema inválido." };
  }
  const accessToken = await requireAccessToken();
  try {
    await setCompanyDefaultTheme(accessToken, companyId, themeCode);
  } catch (error) {
    if (error instanceof ApiError && error.status === 403) {
      return { error: "No tienes permiso para cambiar el tema de la empresa (Companies.Update)." };
    }
    return { error: "No fue posible guardar el tema de la empresa." };
  }
  revalidatePath("/companies");
  return null;
}
