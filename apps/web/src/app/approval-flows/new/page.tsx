import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMe, getRoles } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { CreateApprovalFlowForm } from "./create-approval-flow-form";

export default async function NewApprovalFlowPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const [roles, me] = await Promise.all([
      getRoles(accessToken, { isActive: true, pageSize: 200 }),
      getMe(accessToken),
    ]);

    content =
      roles.items.length === 0 ? (
        <p className="text-muted-foreground text-sm">
          No hay roles activos todavía. Crea al menos un rol en{" "}
          <Link href="/roles" className="text-primary hover:underline">
            Roles
          </Link>{" "}
          antes de configurar un flujo.
        </p>
      ) : (
        <CreateApprovalFlowForm roles={roles.items} companies={me.companies} />
      );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403
          ? "Configurar un flujo requiere también poder consultar roles (Roles.Read)."
          : "No fue posible cargar los datos para crear el flujo."}
      </p>
    );
  }

  return (
    <>
      <AppHeader title="Nuevo flujo de aprobación" subtitle="Qué roles deben aprobar y en qué condiciones." />
    <div className="mx-auto max-w-xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href="/approval-flows" />}>
          ← Volver
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
