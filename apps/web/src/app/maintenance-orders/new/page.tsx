import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssets, getMaintenanceChecklists, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Button } from "@/components/ui/button";
import { OpenMaintenanceOrderForm } from "./open-maintenance-order-form";

export default async function NewMaintenanceOrderPage({
  searchParams,
}: {
  searchParams: Promise<{ companyId?: string; assetId?: string }>;
}) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);

  if (me.companies.length === 0) {
    return <EmptyCompanyState />;
  }

  const companyId = params.companyId && me.companies.some((c) => c.companyId === params.companyId)
    ? params.companyId
    : me.companies[0].companyId;

  let content: React.ReactNode;
  try {
    const [inWarehouse, assigned, checklists] = await Promise.all([
      getAssets(accessToken, { companyId, status: "InWarehouse", pageSize: 200 }),
      getAssets(accessToken, { companyId, status: "Assigned", pageSize: 200 }),
      getMaintenanceChecklists(accessToken),
    ]);

    const eligibleAssets = [...inWarehouse.items, ...assigned.items];
    const activeChecklists = checklists.filter((c) => c.isActive);

    content = (
      <OpenMaintenanceOrderForm assets={eligibleAssets} checklists={activeChecklists} defaultAssetId={params.assetId ?? null} />
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403
          ? "Abrir una orden de mantenimiento requiere permiso Maintenance.Create."
          : "No fue posible cargar los datos para abrir la orden."}
      </p>
    );
  }

  return (
    <div className="mx-auto max-w-md p-8">
      <AppHeader
        title="Nueva orden de mantenimiento"
        subtitle="Envía un activo en almacén o asignado a mantenimiento."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/maintenance-orders?companyId=${companyId}`} />}>
          ← Volver
        </Button>
      </div>
      {content}
    </div>
  );
}
