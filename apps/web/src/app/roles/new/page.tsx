import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { CreateRoleForm } from "./create-role-form";

export default async function NewRolePage() {
  await requireAccessToken();

  return (
    <div className="mx-auto max-w-md p-8">
      <AppHeader title="Nuevo rol" />
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href="/roles" />}>
          ← Volver
        </Button>
      </div>
      <CreateRoleForm />
    </div>
  );
}
