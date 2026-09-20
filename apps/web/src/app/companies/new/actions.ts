"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createCompany } from "@/lib/api";

export type CreateCompanyActionState = { error: string | null };

export async function createCompanyAction(
  _prevState: CreateCompanyActionState,
  formData: FormData,
): Promise<CreateCompanyActionState> {
  const accessToken = await requireAccessToken();

  const legalName = String(formData.get("legalName") ?? "").trim();
  const tradeName = String(formData.get("tradeName") ?? "").trim();
  const taxId = String(formData.get("taxId") ?? "").trim();
  const baseCurrency = String(formData.get("baseCurrency") ?? "").trim().toUpperCase();
  const timeZone = String(formData.get("timeZone") ?? "").trim();

  if (!legalName || !tradeName || !taxId || baseCurrency.length !== 3 || !timeZone) {
    return { error: "Todos los campos son obligatorios y la moneda debe tener 3 letras (ISO 4217)." };
  }

  try {
    await createCompany(accessToken, { legalName, tradeName, taxId, baseCurrency, timeZone });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible crear la empresa." };
    }
    throw error;
  }

  redirect("/companies");
}
