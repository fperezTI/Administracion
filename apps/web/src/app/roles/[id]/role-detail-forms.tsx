"use client";

import { useActionState } from "react";
import type { PermissionModuleGroup, RoleDetail } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  duplicateRoleAction,
  savePermissionsAction,
  updateRoleAction,
  type DuplicateRoleActionState,
  type RoleFormState,
} from "./actions";

const roleFormInitialState: RoleFormState = { error: null, success: false };

export function RenameRoleForm({ role }: { role: RoleDetail }) {
  const boundAction = updateRoleAction.bind(null, role.id);
  const [state, formAction, pending] = useActionState(boundAction, roleFormInitialState);

  return (
    <form action={formAction} className="flex flex-col gap-3 rounded-lg border p-4">
      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1">
          <Label htmlFor="name">Nombre</Label>
          <Input id="name" name="name" defaultValue={role.name} required maxLength={100} />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="description">Descripción</Label>
          <Input id="description" name="description" defaultValue={role.description ?? ""} maxLength={500} />
        </div>
      </div>
      <div className="flex items-center justify-between">
        <p className="text-sm">
          {state.error && <span className="text-destructive">{state.error}</span>}
          {!state.error && state.success && <span className="text-green-600 dark:text-green-500">Guardado.</span>}
        </p>
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? "Guardando…" : "Guardar"}
        </Button>
      </div>
    </form>
  );
}

export function DuplicateRoleForm({ roleId }: { roleId: string }) {
  const boundAction = duplicateRoleAction.bind(null, roleId);
  const initialState: DuplicateRoleActionState = { error: null };
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex items-end gap-2">
      <div className="flex flex-col gap-1">
        <Label htmlFor="newName">Duplicar como</Label>
        <Input id="newName" name="newName" required maxLength={100} placeholder="Nombre del nuevo rol" />
      </div>
      <Button type="submit" variant="outline" size="sm" disabled={pending}>
        {pending ? "Duplicando…" : "Duplicar"}
      </Button>
      {state.error && <p className="text-destructive text-sm">{state.error}</p>}
    </form>
  );
}

export function PermissionMatrixForm({
  roleId,
  groups,
  assignedPermissionIds,
}: {
  roleId: string;
  groups: PermissionModuleGroup[];
  assignedPermissionIds: string[];
}) {
  const boundAction = savePermissionsAction.bind(null, roleId);
  const [state, formAction, pending] = useActionState(boundAction, roleFormInitialState);
  const assigned = new Set(assignedPermissionIds);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="grid gap-4 sm:grid-cols-2">
        {groups.map((group) => (
          <fieldset key={group.module} className="rounded-lg border p-3">
            <legend className="px-1 text-xs font-semibold tracking-wide uppercase">{group.module}</legend>
            <div className="flex flex-col gap-1.5">
              {group.permissions.map((permission) => (
                <label key={permission.id} className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    name="permissionId"
                    value={permission.id}
                    defaultChecked={assigned.has(permission.id)}
                    className="size-4"
                  />
                  <span title={permission.description}>{permission.action}</span>
                </label>
              ))}
            </div>
          </fieldset>
        ))}
      </div>

      <div className="flex items-center justify-between">
        <p className="text-sm">
          {state.error && <span className="text-destructive">{state.error}</span>}
          {!state.error && state.success && <span className="text-green-600 dark:text-green-500">Permisos guardados.</span>}
        </p>
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Guardar matriz de permisos"}
        </Button>
      </div>
    </form>
  );
}
