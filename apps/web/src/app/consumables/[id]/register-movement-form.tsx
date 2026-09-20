"use client";

import { useActionState } from "react";
import type { OrgUnitNode } from "@/lib/api";
import { CONSUMABLE_STOCK_DIRECTION_LABELS, CONSUMABLE_STOCK_MOVEMENT_REASON_LABELS } from "@/lib/maintenance-labels";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/ui/field";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { registerMovementAction, type RegisterMovementActionState } from "./actions";

const initialState: RegisterMovementActionState = { error: null };

export function RegisterMovementForm({ consumableId, warehouses }: { consumableId: string; warehouses: OrgUnitNode[] }) {
  const boundAction = registerMovementAction.bind(null, consumableId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-3">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <Field label="Dirección" htmlFor="direction">
          <Select name="direction" required>
            <SelectTrigger id="direction" className="w-full">
              <SelectValue placeholder="Selecciona">
                {(value: keyof typeof CONSUMABLE_STOCK_DIRECTION_LABELS) => CONSUMABLE_STOCK_DIRECTION_LABELS[value]}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              {Object.entries(CONSUMABLE_STOCK_DIRECTION_LABELS).map(([value, label]) => (
                <SelectItem key={value} value={value}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
        <Field label="Motivo" htmlFor="reason">
          <Select name="reason" required>
            <SelectTrigger id="reason" className="w-full">
              <SelectValue placeholder="Selecciona">
                {(value: keyof typeof CONSUMABLE_STOCK_MOVEMENT_REASON_LABELS) => CONSUMABLE_STOCK_MOVEMENT_REASON_LABELS[value]}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              {Object.entries(CONSUMABLE_STOCK_MOVEMENT_REASON_LABELS).map(([value, label]) => (
                <SelectItem key={value} value={value}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
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
        <Field label="Cantidad" htmlFor="quantity">
          <Input id="quantity" name="quantity" type="number" min={0} step="any" required />
        </Field>
      </div>

      <Field label="Notas (opcional)" htmlFor="notes">
        <Input id="notes" name="notes" maxLength={1000} />
      </Field>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? "Registrando…" : "Registrar movimiento"}
        </Button>
      </div>
    </form>
  );
}
