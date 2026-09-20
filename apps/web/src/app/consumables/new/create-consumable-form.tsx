"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createConsumableAction, type CreateConsumableActionState } from "./actions";

const initialState: CreateConsumableActionState = { error: null };

export function CreateConsumableForm({ companyId }: { companyId: string }) {
  const [state, formAction, pending] = useActionState(createConsumableAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="companyId" value={companyId} />

      <div className="flex flex-col gap-1">
        <Label htmlFor="name">Nombre</Label>
        <Input id="name" name="name" required maxLength={200} placeholder="Tóner HP 58A" />
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="flex flex-col gap-1">
          <Label htmlFor="sku">SKU (opcional)</Label>
          <Input id="sku" name="sku" maxLength={100} placeholder="TN-58A" />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="unitOfMeasure">Unidad de medida</Label>
          <Input id="unitOfMeasure" name="unitOfMeasure" required maxLength={50} placeholder="Pieza" />
        </div>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="minimumStock">Existencia mínima (opcional)</Label>
        <Input id="minimumStock" name="minimumStock" type="number" min={0} step="any" />
      </div>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Registrar consumible"}
        </Button>
      </div>
    </form>
  );
}
