import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssetById } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { RequestDecommissionForm } from "./request-decommission-form";

export default async function RequestDecommissionPage({ params }: { params: Promise<{ id: string }> }) {
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
    <>
      <AppHeader title="Solicitar baja" subtitle={`${asset.internalFolio} — ${asset.brand} ${asset.model}`} />
    <div className="mx-auto max-w-xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/assets/${id}`} />}>
          <ArrowLeft data-icon="inline-start" />
          Volver
        </Button>
      </div>
      <RequestDecommissionForm assetId={id} />
    </div>
    </>
  );
}
