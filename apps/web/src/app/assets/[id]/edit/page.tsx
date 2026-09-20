import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssetById, getAssetCategoryById } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { ContractualInfoForm, FinancialInfoForm, GeneralInfoForm } from "./edit-forms";

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
    <div className="mx-auto flex max-w-3xl flex-col gap-4 p-8">
      <AppHeader title="Activos" />
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold tracking-tight">Editar {asset.internalFolio}</h2>
        <Button variant="outline" render={<Link href={`/assets/${id}`} />}>
          ← Volver al detalle
        </Button>
      </div>

      <GeneralInfoForm asset={asset} category={category} />
      <FinancialInfoForm asset={asset} />
      <ContractualInfoForm asset={asset} />
    </div>
  );
}
