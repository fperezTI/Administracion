"use client";

import { useActionState } from "react";
import type { OrgUnitNode } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createSparePartAction, type CreateSparePartActionState } from "./actions";

const initialState: CreateSparePartActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

export function CreateSparePartForm({ warehouses, companyId }: { warehouses: OrgUnitNode[]; companyId: string }) {
  const [state, formAction, pending] = useActionState(createSparePartAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="companyId" value={companyId} />

      <div className="flex flex-col gap-1">
        <Label htmlFor="name">Nombre</Label>
        <Input id="name" name="name" required maxLength={200} placeholder="Memoria RAM 16GB" />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="partNumber">Número de parte (opcional)</Label>
        <Input id="partNumber" name="partNumber" maxLength={100} placeholder="RAM-16GB" />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="serialNumber">Número de serie</Label>
        <Input id="serialNumber" name="serialNumber" required maxLength={100} />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="warehouseOrgUnitId">Almacén</Label>
        <select id="warehouseOrgUnitId" name="warehouseOrgUnitId" className={selectClassName} required defaultValue="">
          <option value="" disabled>
            Selecciona
          </option>
          {warehouses.map((w) => (
            <option key={w.id} value={w.id}>
              {w.name}
            </option>
          ))}
        </select>
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Registrar refacción"}
        </Button>
      </div>
    </form>
  );
}
