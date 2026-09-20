"use client";

import { useActionState } from "react";
import type { WarrantySummary } from "@/lib/api";
import { WARRANTY_TYPE_LABELS } from "@/lib/maintenance-labels";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { editWarrantyAction, type EditWarrantyActionState } from "./actions";

const initialState: EditWarrantyActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

export function EditWarrantyForm({ warranty, companyId }: { warranty: WarrantySummary; companyId: string }) {
  const boundAction = editWarrantyAction.bind(null, warranty.id);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="companyId" value={companyId} />

      <div className="flex flex-col gap-1">
        <Label htmlFor="type">Tipo</Label>
        <select id="type" name="type" className={selectClassName} required defaultValue={warranty.type}>
          {Object.entries(WARRANTY_TYPE_LABELS).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="provider">Proveedor</Label>
        <Input id="provider" name="provider" required maxLength={200} defaultValue={warranty.provider} />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1">
          <Label htmlFor="startDate">Inicio</Label>
          <Input id="startDate" name="startDate" type="date" required defaultValue={warranty.startDate} />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="endDate">Fin</Label>
          <Input id="endDate" name="endDate" type="date" required defaultValue={warranty.endDate} />
        </div>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="terms">Términos (opcional)</Label>
        <Input id="terms" name="terms" maxLength={2000} defaultValue={warranty.terms ?? ""} />
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Guardar cambios"}
        </Button>
      </div>
    </form>
  );
}
