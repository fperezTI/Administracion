import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { getAssets, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Button } from "@/components/ui/button";
import { CreateRequestForm } from "./create-request-form";

export default async function NewInternalRequestPage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);

  if (me.companies.length === 0) {
    return <EmptyCompanyState />;
  }

  const companyId = params.companyId && me.companies.some((c) => c.companyId === params.companyId)
    ? params.companyId
    : me.companies[0].companyId;

  const [inWarehouse, assigned] = await Promise.all([
    getAssets(accessToken, { companyId, status: "InWarehouse", pageSize: 200 }),
    getAssets(accessToken, { companyId, status: "Assigned", pageSize: 200 }),
  ]);

  return (
    <div className="mx-auto max-w-md p-8">
      <AppHeader
        title="Nueva solicitud"
        subtitle="Pide que te asignen un activo, un préstamo, o reporta que necesita mantenimiento."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/requests?companyId=${companyId}`} />}>
          ← Volver
        </Button>
      </div>
      <CreateRequestForm inWarehouseAssets={inWarehouse.items} assignedAssets={assigned.items} companyId={companyId} />
    </div>
  );
}
