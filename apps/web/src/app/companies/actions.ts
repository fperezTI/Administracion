"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { setCompanyActive } from "@/lib/api";

export async function toggleCompanyActiveAction(companyId: string, isActive: boolean, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await setCompanyActive(accessToken, companyId, isActive);
  revalidatePath("/companies");
}
