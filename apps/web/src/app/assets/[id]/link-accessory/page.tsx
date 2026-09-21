import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssetById, getEligibleAccessoryCandidates } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { LinkAccessoryForm } from "./link-accessory-form";

export default async function LinkAccessoryPage({ params }: { params: Promise<{ id: string }> }) {
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

  if (asset.accessoryOfAssetId !== null) {
    notFound();
  }

  const candidates = await getEligibleAccessoryCandidates(accessToken, asset.companyId, id);

  return (
    <>
      <AppHeader
        title="Vincular accesorio"
        subtitle={`${asset.internalFolio} — ${asset.brand} ${asset.model}`}
      />
    <div className="mx-auto max-w-xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/assets/${id}`} />}>
          ← Volver
        </Button>
      </div>
      <p className="text-muted-foreground mb-4 text-sm">
        Al vincular un activo como accesorio, se incluirá automáticamente cuando asignes o reasignes{" "}
        {asset.internalFolio}.
      </p>
      <LinkAccessoryForm primaryAssetId={id} candidates={candidates} />
    </div>
    </>
  );
}
