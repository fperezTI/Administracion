"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { markNotificationAsRead } from "@/lib/api";

export async function markAsReadAction(notificationId: string, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await markNotificationAsRead(accessToken, notificationId);
  revalidatePath("/notifications");
}
