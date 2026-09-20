import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { getAssets, getMe, getUsers } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Button } from "@/components/ui/button";
import { CreateLoanForm } from "./create-loan-form";

export default async function NewLoanPage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);

  if (me.companies.length === 0) {
    return <EmptyCompanyState />;
  }

  const companyId = params.companyId && me.companies.some((c) => c.companyId === params.companyId)
    ? params.companyId
    : me.companies[0].companyId;

  const [assets, users] = await Promise.all([
    getAssets(accessToken, { companyId, status: "InWarehouse", pageSize: 200 }),
    getUsers(accessToken, { pageSize: 200 }),
  ]);

  return (
    <div className="mx-auto max-w-md p-8">
      <AppHeader title="Nuevo préstamo" subtitle="Préstamo de corto plazo, sin firma de recepción." />
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/loans?companyId=${companyId}`} />}>
          ← Volver
        </Button>
      </div>
      <CreateLoanForm assets={assets.items} users={users.items} />
    </div>
  );
}
