"use server";

import { redirect } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, createApprovalFlow, type ApprovalMode } from "@/lib/api";

export type CreateApprovalFlowActionState = { error: string | null };

export async function createApprovalFlowAction(
  _prevState: CreateApprovalFlowActionState,
  formData: FormData,
): Promise<CreateApprovalFlowActionState> {
  const accessToken = await requireAccessToken();

  const key = String(formData.get("key") ?? "").trim();
  const companyId = String(formData.get("companyId") ?? "").trim() || null;
  const approverRoleIds = formData.getAll("approverRoleIds").map(String).filter(Boolean);
  const requiredApprovals = Number(formData.get("requiredApprovals") ?? 1);
  const mode = String(formData.get("mode") ?? "Parallel") as ApprovalMode;
  const requiresComment = formData.get("requiresComment") === "on";

  if (!key || approverRoleIds.length === 0) {
    return { error: "La clave y al menos un rol aprobador son obligatorios." };
  }

  try {
    await createApprovalFlow(accessToken, { key, companyId, approverRoleIds, requiredApprovals, mode, requiresComment });
  } catch (error) {
    if (error instanceof ApiError) {
      return { error: error.detail ?? "No fue posible crear el flujo de aprobación." };
    }
    throw error;
  }

  redirect("/approval-flows");
}
