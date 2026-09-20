import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssetCategories, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Button } from "@/components/ui/button";
import { UploadImportBatchForm } from "./upload-import-batch-form";

export default async function NewImportBatchPage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
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
    const categories = await getAssetCategories(accessToken, { pageSize: 200 });
    content = <UploadImportBatchForm companyId={companyId} categories={categories.items.filter((c) => c.isActive)} />;
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "Iniciar una importación requiere permiso Imports.Create." : "No fue posible cargar las categorías de activos."}
      </p>
    );
  }

  return (
    <div className="mx-auto max-w-md p-8">
      <AppHeader
        title="Nueva importación"
        subtitle="Sube un archivo CSV para dar de alta activos en lote."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/imports?companyId=${companyId}`} />}>
          ← Volver
        </Button>
      </div>
      {content}
    </div>
  );
}
