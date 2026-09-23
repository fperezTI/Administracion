import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getPermissions, getRoleById } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DuplicateRoleForm, PermissionMatrixForm, RenameRoleForm } from "./role-detail-forms";
import { toggleRoleActiveAction } from "./actions";

export default async function RoleDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let role;
  try {
    role = await getRoleById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  const groups = await getPermissions(accessToken);

  return (
    <>
      <AppHeader title="Roles" />
    <div className="mx-auto flex max-w-3xl flex-col gap-4 px-8 pb-8">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold tracking-tight">{role.name}</h2>
        <div className="flex items-center gap-2">
          <Badge variant={role.isActive ? "success" : "outline"}>{role.isActive ? "Activo" : "Inactivo"}</Badge>
          <form action={toggleRoleActiveAction.bind(null, id, !role.isActive)}>
            <Button type="submit" variant="outline" size="sm">
              {role.isActive ? "Desactivar" : "Activar"}
            </Button>
          </form>
        </div>
      </div>

      <RenameRoleForm role={role} />
      <DuplicateRoleForm roleId={id} />
      <PermissionMatrixForm roleId={id} groups={groups} assignedPermissionIds={role.permissionIds} />

      <Button variant="outline" render={<Link href="/roles" />} className="self-start">
        <ArrowLeft data-icon="inline-start" />
        Volver
      </Button>
    </div>
    </>
  );
}
