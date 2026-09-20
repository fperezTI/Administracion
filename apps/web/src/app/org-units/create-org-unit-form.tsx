"use client";

import { useActionState } from "react";
import type { OrgUnitTypeSummary } from "@/lib/api";
import type { OrgUnitOption } from "@/lib/org-unit-tree";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/ui/field";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createOrgUnitAction, type CreateOrgUnitActionState } from "./actions";

const initialState: CreateOrgUnitActionState = { error: null };

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
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <Field label="Nombre" htmlFor="name">
          <Input id="name" name="name" required maxLength={200} placeholder="p. ej. Almacén Central" />
        </Field>
        <Field label="Código" htmlFor="code">
          <Input id="code" name="code" required maxLength={50} placeholder="p. ej. ALM-01" />
        </Field>
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <Field label="Tipo" htmlFor="orgUnitTypeId">
          <Select name="orgUnitTypeId" required>
            <SelectTrigger id="orgUnitTypeId" className="w-full">
              <SelectValue placeholder="Selecciona">{(value: string) => types.find((t) => t.id === value)?.name}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              {types.map((type) => (
                <SelectItem key={type.id} value={type.id}>
                  {type.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
        <Field label="Unidad padre" htmlFor="parentOrgUnitId">
          <Select name="parentOrgUnitId" defaultValue="">
            <SelectTrigger id="parentOrgUnitId" className="w-full">
              <SelectValue>
                {(value: string) =>
                  value === "" ? "Ninguna (raíz)" : orgUnitOptions.find((o) => o.id === value)?.label
                }
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">Ninguna (raíz)</SelectItem>
              {orgUnitOptions.map((option) => (
                <SelectItem key={option.id} value={option.id}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
      </div>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? "Creando…" : "Crear"}
        </Button>
      </div>
    </form>
  );
}
