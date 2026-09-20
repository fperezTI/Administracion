import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { CreateTemplateForm } from "./create-template-form";

export default async function NewTemplatePage() {
  await requireAccessToken();

  return (
    <div className="mx-auto max-w-lg p-8">
      <AppHeader title="Nueva plantilla" />
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href="/templates" />}>
          ← Volver
        </Button>
      </div>
      <CreateTemplateForm />
    </div>
  );
}
