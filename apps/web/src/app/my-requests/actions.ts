"use server";

import { revalidatePath } from "next/cache";
import { requireAccessToken } from "@/lib/require-session";
import { cancelInternalRequest } from "@/lib/api";

export async function cancelMyRequestAction(internalRequestId: string, _formData: FormData) {
  const accessToken = await requireAccessToken();
  await cancelInternalRequest(accessToken, internalRequestId);
  revalidatePath("/my-requests");
}
