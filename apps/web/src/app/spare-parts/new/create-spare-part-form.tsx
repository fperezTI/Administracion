"use client";

import { useActionState } from "react";
import type { OrgUnitNode } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/ui/field";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createSparePartAction, type CreateSparePartActionState } from "./actions";

const initialState: CreateSparePartActionState = { error: null };

export function CreateSparePartForm({ warehouses, companyId }: { warehouses: OrgUnitNode[]; companyId: string }) {
  const [state, formAction, pending] = useActionState(createSparePartAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="companyId" value={companyId} />

      <Field label="Nombre" htmlFor="name">
        <Input id="name" name="name" required maxLength={200} placeholder="Memoria RAM 16GB" />
      </Field>

      <Field label="Número de parte (opcional)" htmlFor="partNumber">
        <Input id="partNumber" name="partNumber" maxLength={100} placeholder="RAM-16GB" />
      </Field>

      <Field label="Número de serie" htmlFor="serialNumber">
        <Input id="serialNumber" name="serialNumber" required maxLength={100} />
      </Field>

      <Field label="Almacén" htmlFor="warehouseOrgUnitId">
        <Select name="warehouseOrgUnitId" required>
          <SelectTrigger id="warehouseOrgUnitId" className="w-full">
            <SelectValue placeholder="Selecciona">{(value: string) => warehouses.find((w) => w.id === value)?.name}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            {warehouses.map((w) => (
              <SelectItem key={w.id} value={w.id}>
                {w.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Registrar refacción"}
        </Button>
      </div>
    </form>
  );
}
