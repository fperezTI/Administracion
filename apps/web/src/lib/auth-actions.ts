"use server";

import { redirect } from "next/navigation";
import { signOut } from "@/auth";

/** Clears the local session and also ends the session at Entra ID (federated logout) — without this,
 * "cerrar sesión" only forgets the app's cookie; the browser would still be signed in to Microsoft. */
export async function federatedSignOut() {
  await signOut({ redirect: false });
  const baseUrl = process.env.AUTH_URL ?? "http://localhost:3000";
  const tenantId = process.env.ENTRA_TENANT_ID;
  redirect(
    `https://login.microsoftonline.com/${tenantId}/oauth2/v2.0/logout?post_logout_redirect_uri=${encodeURIComponent(baseUrl)}`,
  );
}
