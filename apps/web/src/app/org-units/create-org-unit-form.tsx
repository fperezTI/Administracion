"use client";

import { useActionState } from "react";
import type { OrgUnitTypeSummary } from "@/lib/api";
import type { OrgUnitOption } from "@/lib/org-unit-tree";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createOrgUnitAction, type CreateOrgUnitActionState } from "./actions";

const initialState: CreateOrgUnitActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none dark:bg-input/30";

export function CreateOrgUnitForm({
  companyId,
  types,
  orgUnitOptions,
}: {
  companyId: string;
  types: OrgUnitTypeSummary[];
  orgUnitOptions: OrgUnitOption[];
}) {
  const boundAction = createOrgUnitAction.bind(null, companyId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-3 rounded-lg border p-4">
      <p className="text-sm font-medium">Nueva unidad organizacional</p>
      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1">
          <Label htmlFor="name">Nombre</Label>
          <Input id="name" name="name" required maxLength={200} placeholder="p. ej. Almacén Central" />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="code">Código</Label>
          <Input id="code" name="code" required maxLength={50} placeholder="p. ej. ALM-01" />
        </div>
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1">
          <Label htmlFor="orgUnitTypeId">Tipo</Label>
          <select id="orgUnitTypeId" name="orgUnitTypeId" className={selectClassName} required defaultValue="">
            <option value="" disabled>
              Selecciona
            </option>
            {types.map((type) => (
              <option key={type.id} value={type.id}>
                {type.name}
              </option>
            ))}
          </select>
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="parentOrgUnitId">Unidad padre</Label>
          <select id="parentOrgUnitId" name="parentOrgUnitId" className={selectClassName} defaultValue="">
            <option value="">Ninguna (raíz)</option>
            {orgUnitOptions.map((option) => (
              <option key={option.id} value={option.id}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? "Creando…" : "Crear"}
        </Button>
      </div>
    </form>
  );
}
