"use client";

import { useActionState } from "react";
import type { WarrantySummary } from "@/lib/api";
import { WARRANTY_TYPE_LABELS } from "@/lib/maintenance-labels";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/ui/field";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { editWarrantyAction, type EditWarrantyActionState } from "./actions";

const initialState: EditWarrantyActionState = { error: null };

export function EditWarrantyForm({ warranty, companyId }: { warranty: WarrantySummary; companyId: string }) {
  const boundAction = editWarrantyAction.bind(null, warranty.id);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="companyId" value={companyId} />

      <Field label="Tipo" htmlFor="type">
        <Select name="type" required defaultValue={warranty.type}>
          <SelectTrigger id="type" className="w-full">
            <SelectValue>{(value: keyof typeof WARRANTY_TYPE_LABELS) => WARRANTY_TYPE_LABELS[value]}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            {Object.entries(WARRANTY_TYPE_LABELS).map(([value, label]) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>

      <Field label="Proveedor" htmlFor="provider">
        <Input id="provider" name="provider" required maxLength={200} defaultValue={warranty.provider} />
      </Field>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Field label="Inicio" htmlFor="startDate">
          <Input id="startDate" name="startDate" type="date" required defaultValue={warranty.startDate} />
        </Field>
        <Field label="Fin" htmlFor="endDate">
          <Input id="endDate" name="endDate" type="date" required defaultValue={warranty.endDate} />
        </Field>
      </div>

      <Field label="Términos (opcional)" htmlFor="terms">
        <Input id="terms" name="terms" maxLength={2000} defaultValue={warranty.terms ?? ""} />
      </Field>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Guardar cambios"}
        </Button>
      </div>
    </form>
  );
}
