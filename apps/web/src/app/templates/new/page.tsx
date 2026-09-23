import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { CreateTemplateForm } from "./create-template-form";

export default async function NewTemplatePage() {
  await requireAccessToken();

  return (
    <>
      <AppHeader title="Nueva plantilla" />
    <div className="mx-auto max-w-xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href="/templates" />}>
          <ArrowLeft data-icon="inline-start" />
          Volver
        </Button>
      </div>
      <CreateTemplateForm />
    </div>
    </>
  );
}
