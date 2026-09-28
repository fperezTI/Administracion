import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssetById, getAssetCategoryById } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { ContractualInfoForm, FinancialInfoForm, GeneralInfoForm, MaintenanceScheduleForm } from "./edit-forms";
import { BackToDetailLink, UnsavedChangesProvider } from "./unsaved-changes";

export default async function EditAssetPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let asset;
  try {
    asset = await getAssetById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  const category = await getAssetCategoryById(accessToken, asset.assetCategoryId).catch(() => null);

  return (
    <>
      <AppHeader title="Activos" />
    <UnsavedChangesProvider>
    <div className="mx-auto flex max-w-3xl flex-col gap-4 px-8 pb-8">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold tracking-tight">Editar {asset.internalFolio}</h2>
        <BackToDetailLink href={`/assets/${id}`} />
      </div>

      <GeneralInfoForm asset={asset} category={category} />
      <FinancialInfoForm asset={asset} />
      <ContractualInfoForm asset={asset} />
      <MaintenanceScheduleForm asset={asset} />
    </div>
    </UnsavedChangesProvider>
    </>
  );
}
