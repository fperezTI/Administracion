import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Button } from "@/components/ui/button";
import { CreateConsumableForm } from "./create-consumable-form";

export default async function NewConsumablePage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);

  if (me.companies.length === 0) {
    return <EmptyCompanyState />;
  }

  const companyId = params.companyId && me.companies.some((c) => c.companyId === params.companyId)
    ? params.companyId
    : me.companies[0].companyId;

  return (
    <div className="mx-auto max-w-md p-8">
      <AppHeader
        title="Nuevo consumible"
        subtitle="Se registra con existencia inicial en cero."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/consumables?companyId=${companyId}`} />}>
          ← Volver
        </Button>
      </div>
      <CreateConsumableForm companyId={companyId} />
    </div>
  );
}
