"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { setApprovalFlowActive } from "@/lib/api";

export async function toggleApprovalFlowActiveAction(flowDefinitionId: string, isActive: boolean, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await setApprovalFlowActive(accessToken, flowDefinitionId, isActive);
  revalidatePath("/approval-flows");
}
