import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { getAssetCategories } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { CreateChecklistForm } from "./create-checklist-form";

export default async function NewMaintenanceChecklistPage() {
  const accessToken = await requireAccessToken();
  const categories = await getAssetCategories(accessToken, { isActive: true, pageSize: 200 });

  return (
    <>
      <AppHeader title="Nuevo checklist" subtitle="Un ítem por línea." />
    <div className="mx-auto max-w-xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href="/maintenance-checklists" />}>
          ← Volver
        </Button>
      </div>
      <CreateChecklistForm categories={categories.items} />
    </div>
    </>
  );
}
