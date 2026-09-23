import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssets, getCompanies, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Button } from "@/components/ui/button";
import { CreateTransferForm } from "./create-transfer-form";

export default async function NewTransferPage({
  searchParams,
}: {
  searchParams: Promise<{ companyId?: string }>;
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
    const [assets, companies] = await Promise.all([
      getAssets(accessToken, { companyId, status: "InWarehouse", pageSize: 200 }),
      getCompanies(accessToken, { isActive: true, pageSize: 50 }),
    ]);

    const destinationCompanies = companies.items.filter((c) => c.id !== companyId);
    content = <CreateTransferForm assets={assets.items} companies={destinationCompanies} />;
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403
          ? "Solicitar una transferencia requiere también poder consultar empresas (Companies.Read)."
          : "No fue posible cargar los datos para solicitar la transferencia."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Nueva transferencia"
        subtitle="Envía un activo en almacén a otra empresa del tenant."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/transfers?companyId=${companyId}`} />}>
          <ArrowLeft data-icon="inline-start" />
          Volver
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
