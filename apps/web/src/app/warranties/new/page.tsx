import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { getAssets, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Button } from "@/components/ui/button";
import { CreateWarrantyForm } from "./create-warranty-form";

export default async function NewWarrantyPage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);

  if (me.companies.length === 0) {
    return <EmptyCompanyState />;
  }

  const companyId = params.companyId && me.companies.some((c) => c.companyId === params.companyId)
    ? params.companyId
    : me.companies[0].companyId;

  const assets = await getAssets(accessToken, { companyId, pageSize: 200 });

  return (
    <>
      <AppHeader
        title="Nueva garantía"
        subtitle="Registra una cobertura de garantía o soporte para un activo."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/warranties?companyId=${companyId}`} />}>
          ← Volver
        </Button>
      </div>
      <CreateWarrantyForm assets={assets.items} companyId={companyId} />
    </div>
    </>
  );
}
