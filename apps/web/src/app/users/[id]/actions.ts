"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import {
  anonymizeUser,
  assignRoleToUser,
  grantUserCompanyAccess,
  removeRoleFromUser,
  revokeUserCompanyAccess,
} from "@/lib/api";

export async function assignRoleAction(userId: string, formData: FormData) {
  const accessToken = await requireAccessToken();
  const roleId = String(formData.get("roleId") ?? "");
  if (!roleId) return;
  await assignRoleToUser(accessToken, userId, roleId);
  revalidatePath(`/users/${userId}`);
}

export async function removeRoleAction(userId: string, roleId: string, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await removeRoleFromUser(accessToken, userId, roleId);
  revalidatePath(`/users/${userId}`);
}

export async function grantCompanyAction(userId: string, formData: FormData) {
  const accessToken = await requireAccessToken();
  const companyId = String(formData.get("companyId") ?? "");
  if (!companyId) return;
  await grantUserCompanyAccess(accessToken, userId, companyId);
  revalidatePath(`/users/${userId}`);
}

export async function revokeCompanyAction(userId: string, companyId: string, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await revokeUserCompanyAccess(accessToken, userId, companyId);
  revalidatePath(`/users/${userId}`);
}

export async function anonymizeUserAction(userId: string, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await anonymizeUser(accessToken, userId);
  revalidatePath(`/users/${userId}`);
}
