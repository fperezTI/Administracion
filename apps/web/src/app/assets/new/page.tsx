import { requireAccessToken } from "@/lib/require-session";
import { getAssetCategories, getAssetCategoryById, getMe, getOrgUnitTree } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { CreateAssetForm } from "./create-asset-form";

export default async function NewAssetPage({
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

  const activeCategories = await getAssetCategories(accessToken, { isActive: true, pageSize: 200 });
  const [categories, orgUnits] = await Promise.all([
    Promise.all(activeCategories.items.map((category) => getAssetCategoryById(accessToken, category.id))),
    getOrgUnitTree(accessToken, companyId),
  ]);

  return (
    <div className="mx-auto max-w-2xl p-8">
      <AppHeader
        title="Nuevo activo"
        subtitle="Alta y etiquetado — se genera folio y etiqueta al guardar."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />

      {categories.length === 0 ? (
        <p className="text-muted-foreground text-sm">
          No hay categorías de activos activas todavía. Pide a un administrador que las configure.
        </p>
      ) : (
        <CreateAssetForm companyId={companyId} categories={categories} orgUnits={orgUnits} />
      )}
    </div>
  );
}
