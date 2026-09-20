import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { CreateCategoryForm } from "./create-category-form";

export default async function NewAssetCategoryPage() {
  await requireAccessToken();

  return (
    <div className="mx-auto max-w-md p-8">
      <AppHeader title="Nueva categoría" />
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href="/asset-categories" />}>
          ← Volver
        </Button>
      </div>
      <CreateCategoryForm />
    </div>
  );
}
