import { getTranslations } from "next-intl/server";
import { requireAccessToken } from "@/lib/require-session";
import { AppHeader } from "@/components/app-header";
import { StatTile } from "@/components/ui/stat-tile";
import { ApiError, getExecutiveDashboard, getMe } from "@/lib/api";

/** Único permiso que controla la visibilidad de este tablero — quien no lo tenga ve la página
 * en blanco (sin datos operativos ni de perfil), a propósito: es la vista de entrada del sistema
 * y no todos los usuarios deben ver métricas de toda la operación. */
const EXECUTIVE_DASHBOARD_PERMISSION = "Dashboards.ViewExecutive";

export default async function DashboardPage() {
  const accessToken = await requireAccessToken();
  const t = await getTranslations("Dashboard");

  let body: React.ReactNode = null;
  try {
    const me = await getMe(accessToken);

    if (me.permissionCodes.includes(EXECUTIVE_DASHBOARD_PERMISSION)) {
      const kpis = await getExecutiveDashboard(accessToken, me.activeCompanyId ?? undefined);
      body = (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          <StatTile label={t("totalAssets")} value={kpis.totalAssets.toLocaleString("es-MX")} />
          <StatTile label={t("assignedAssets")} value={kpis.assignedAssets.toLocaleString("es-MX")} />
          <StatTile label={t("availableAssets")} value={kpis.availableAssets.toLocaleString("es-MX")} />
          <StatTile
            label={t("pendingApprovals")}
            value={kpis.pendingApprovals.toLocaleString("es-MX")}
            tone={kpis.pendingApprovals > 0 ? "warning" : "neutral"}
          />
          <StatTile label={t("openMaintenanceOrders")} value={kpis.openMaintenanceOrders.toLocaleString("es-MX")} />
          <StatTile
            label={t("expiringWarranties")}
            value={kpis.expiringWarranties.toLocaleString("es-MX")}
            tone={kpis.expiringWarranties > 0 ? "warning" : "neutral"}
          />
          <StatTile
            label={t("lowStockConsumables")}
            value={kpis.lowStockConsumables.toLocaleString("es-MX")}
            tone={kpis.lowStockConsumables > 0 ? "destructive" : "neutral"}
          />
        </div>
      );
    }
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    body = status === 403 ? <p className="text-destructive text-sm">{t("accountDisabled")}</p> : <p className="text-destructive text-sm">{t("apiError")}</p>;
  }

  return (
    <>
      <AppHeader title={t("title")} />
    <div className="mx-auto flex max-w-6xl flex-col gap-6 px-8 pb-8">
      {body}
    </div>
    </>
  );
}
