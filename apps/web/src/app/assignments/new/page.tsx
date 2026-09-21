import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { getAssets, getMe, getOrgUnitTree, getUsers } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Button } from "@/components/ui/button";
import { CreateAssignmentForm } from "./create-assignment-form";

export default async function NewAssignmentPage({
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

  const [assets, users, orgUnits] = await Promise.all([
    getAssets(accessToken, { companyId, status: "InWarehouse", pageSize: 200 }),
    getUsers(accessToken, { pageSize: 200 }),
    getOrgUnitTree(accessToken, companyId),
  ]);

  return (
    <>
      <AppHeader title="Nueva asignación" subtitle="Entrega un activo en resguardo a una persona." />
    <div className="mx-auto max-w-xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/assignments?companyId=${companyId}`} />}>
          ← Volver
        </Button>
      </div>
      <CreateAssignmentForm
        assets={assets.items}
        users={users.items}
        orgUnits={orgUnits}
        defaultAssetId={params.assetId ?? null}
      />
    </div>
    </>
  );
}
