import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { getMe, getOrgUnitTree } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Button } from "@/components/ui/button";
import { CreateSparePartForm } from "./create-spare-part-form";

export default async function NewSparePartPage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);

  if (me.companies.length === 0) {
    return <EmptyCompanyState />;
  }

  const companyId = params.companyId && me.companies.some((c) => c.companyId === params.companyId)
    ? params.companyId
    : me.companies[0].companyId;

  const orgUnits = await getOrgUnitTree(accessToken, companyId);
  const warehouses = orgUnits.filter((o) => o.orgUnitTypeName === "Almacén");

  return (
    <>
      <AppHeader
        title="Nueva refacción"
        subtitle="Refacción serializada, se registra en existencia."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/spare-parts?companyId=${companyId}`} />}>
          <ArrowLeft data-icon="inline-start" />
          Volver
        </Button>
      </div>
      {warehouses.length === 0 ? (
        <p className="text-muted-foreground text-sm">
          No hay unidades de tipo Almacén en la estructura organizacional. Crea una en{" "}
          <Link href="/org-units" className="text-primary hover:underline">
            Estructura
          </Link>{" "}
          antes de registrar refacciones.
        </p>
      ) : (
        <CreateSparePartForm warehouses={warehouses} companyId={companyId} />
      )}
    </div>
    </>
  );
}
