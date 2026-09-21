"use client";

import { useActionState } from "react";
import type { CompanySummary, RoleSummary } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { AssignSelect } from "../[id]/assign-select";
import { createUserFromDirectoryAction, type CreateUserFromDirectoryActionState } from "./actions";

const initialState: CreateUserFromDirectoryActionState = { error: null };

export function CreateUserForm({
  entraObjectId,
  displayName,
  email,
  roles,
  companies,
}: {
  entraObjectId: string;
  displayName: string;
  email: string;
  roles: RoleSummary[];
  companies: CompanySummary[];
}) {
  const [state, formAction, pending] = useActionState(createUserFromDirectoryAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="entraObjectId" value={entraObjectId} />
      <input type="hidden" name="displayName" value={displayName} />
      <input type="hidden" name="email" value={email} />

      <div className="flex flex-col gap-1">
        <Label htmlFor="roleId">Rol inicial</Label>
        <AssignSelect
          name="roleId"
          placeholder="Selecciona un rol"
          options={roles.map((r) => ({ id: r.id, label: r.name }))}
        />
      </div>

      <div className="flex flex-col gap-2">
        <Label>Empresas con acceso</Label>
        {companies.length === 0 ? (
          <p className="text-muted-foreground text-xs">No hay empresas dadas de alta todavía.</p>
        ) : (
          companies.map((company) => (
            <label key={company.id} className="flex items-center gap-2 text-sm">
              <input type="checkbox" name="companyIds" value={company.id} className="size-4" />
              {company.tradeName}
            </label>
          ))
        )}
      </div>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Agregando…" : "Agregar usuario"}
        </Button>
      </div>
    </form>
  );
}
