import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssetById } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { RequestDisposalForm } from "./request-disposal-form";

export default async function RequestDisposalPage({ params }: { params: Promise<{ id: string }> }) {
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

  return (
    <div className="mx-auto max-w-md p-8">
      <AppHeader title="Solicitar disposición" subtitle={`${asset.internalFolio} — ${asset.brand} ${asset.model}`} />
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/assets/${id}`} />}>
          ← Volver
        </Button>
      </div>
      <RequestDisposalForm assetId={id} />
    </div>
  );
}
