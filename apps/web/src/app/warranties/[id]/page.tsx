import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { getWarranties, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Button } from "@/components/ui/button";
import { EditWarrantyForm } from "./edit-warranty-form";

export default async function WarrantyDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ companyId?: string }>;
}) {
  const accessToken = await requireAccessToken();
  const { id } = await params;
  const searchParamsValue = await searchParams;
  const me = await getMe(accessToken);

  if (me.companies.length === 0) {
    return <EmptyCompanyState />;
  }

  const companyId = searchParamsValue.companyId && me.companies.some((c) => c.companyId === searchParamsValue.companyId)
    ? searchParamsValue.companyId
    : me.companies[0].companyId;

  const warranties = await getWarranties(accessToken, { companyId });
  const warranty = warranties.find((w) => w.id === id);
  if (!warranty) {
    notFound();
  }

  return (
    <>
      <AppHeader title="Editar garantía" subtitle={warranty.assetFolio} />
    <div className="mx-auto max-w-3xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/warranties?companyId=${companyId}`} />}>
          <ArrowLeft data-icon="inline-start" />
          Volver
        </Button>
      </div>
      <EditWarrantyForm warranty={warranty} companyId={companyId} />
    </div>
    </>
  );
}
