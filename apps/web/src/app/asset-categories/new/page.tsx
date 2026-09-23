import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { CreateCategoryForm } from "./create-category-form";

export default async function NewAssetCategoryPage() {
  await requireAccessToken();

  return (
    <>
      <AppHeader title="Nueva categoría" />
    <div className="mx-auto max-w-xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href="/asset-categories" />}>
          <ArrowLeft data-icon="inline-start" />
          Volver
        </Button>
      </div>
      <CreateCategoryForm />
    </div>
    </>
  );
}
