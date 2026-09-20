import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { CreateCompanyForm } from "./create-company-form";

export default async function NewCompanyPage() {
  await requireAccessToken();

  return (
    <>
      <AppHeader title="Nueva empresa" />
    <div className="mx-auto max-w-xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href="/companies" />}>
          ← Volver
        </Button>
      </div>
      <CreateCompanyForm />
    </div>
    </>
  );
}
