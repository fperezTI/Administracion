import { getTranslations } from "next-intl/server";
import { requireAccessToken } from "@/lib/require-session";
import { AppHeader } from "@/components/app-header";
import { ApiError, getMe } from "@/lib/api";

export default async function DashboardPage() {
  const accessToken = await requireAccessToken();
  const t = await getTranslations("Dashboard");

  let body: React.ReactNode;
  try {
    const me = await getMe(accessToken);
    body = (
      <div className="flex flex-col gap-4">
        <div className="rounded-lg border px-4 py-3 text-sm">
          <p className="text-muted-foreground mb-1 font-medium">{t("profileHeading")}</p>
          <p>
            {me.displayName} — {me.email}
          </p>
        </div>
        <div className="rounded-lg border px-4 py-3 text-sm">
          <p className="text-muted-foreground mb-1 font-medium">{t("permissionsHeading")}</p>
          {me.permissionCodes.length > 0 ? (
            <ul className="list-inside list-disc">
              {me.permissionCodes.map((code) => (
                <li key={code}>{code}</li>
              ))}
            </ul>
          ) : (
            <p>{t("noPermissions")}</p>
          )}
        </div>
        <div className="rounded-lg border px-4 py-3 text-sm">
          <p className="text-muted-foreground mb-1 font-medium">{t("companiesHeading")}</p>
          {me.companies.length > 0 ? (
            <ul className="list-inside list-disc">
              {me.companies.map((company) => (
                <li key={company.companyId}>{company.tradeName}</li>
              ))}
            </ul>
          ) : (
            <p>{t("noCompanies")}</p>
          )}
        </div>
      </div>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    body = <p className="text-destructive text-sm">{status === 403 ? t("accountDisabled") : t("apiError")}</p>;
  }

  return (
    <div className="mx-auto flex max-w-lg flex-col gap-6 p-8">
      <AppHeader title={t("title")} />
      {body}
    </div>
  );
}
