export function formatDateTime(isoUtc: string, timeZone: string): string {
  return new Date(isoUtc).toLocaleString("es-MX", { timeZone });
}

export function formatDate(isoUtc: string, timeZone: string): string {
  return new Date(isoUtc).toLocaleDateString("es-MX", { timeZone });
}

/** companies is usually me.companies (GetMeQuery) — falls back to the user's first company when
 * companyId isn't among them (tenant-wide views with no single owning company), and to "UTC" only as a
 * last-resort technical default, never an assumed real zone. */
export function resolveTimeZone(
  companies: { companyId: string; timeZone: string }[],
  companyId: string | null | undefined,
): string {
  return companies.find((c) => c.companyId === companyId)?.timeZone ?? companies[0]?.timeZone ?? "UTC";
}
