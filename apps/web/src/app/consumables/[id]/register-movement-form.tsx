"use client";

import { useActionState } from "react";
import type { OrgUnitNode } from "@/lib/api";
import { CONSUMABLE_STOCK_DIRECTION_LABELS, CONSUMABLE_STOCK_MOVEMENT_REASON_LABELS } from "@/lib/maintenance-labels";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { registerMovementAction, type RegisterMovementActionState } from "./actions";

const initialState: RegisterMovementActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

export function RegisterMovementForm({ consumableId, warehouses }: { consumableId: string; warehouses: OrgUnitNode[] }) {
  const boundAction = registerMovementAction.bind(null, consumableId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-3">
      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1">
          <Label htmlFor="direction">Dirección</Label>
          <select id="direction" name="direction" className={selectClassName} required defaultValue="">
            <option value="" disabled>
              Selecciona
            </option>
            {Object.entries(CONSUMABLE_STOCK_DIRECTION_LABELS).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="reason">Motivo</Label>
          <select id="reason" name="reason" className={selectClassName} required defaultValue="">
            <option value="" disabled>
              Selecciona
            </option>
            {Object.entries(CONSUMABLE_STOCK_MOVEMENT_REASON_LABELS).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
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
        <div className="flex flex-col gap-1">
          <Label htmlFor="quantity">Cantidad</Label>
          <Input id="quantity" name="quantity" type="number" min={0} step="any" required />
        </div>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="notes">Notas (opcional)</Label>
        <Input id="notes" name="notes" maxLength={1000} />
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? "Registrando…" : "Registrar movimiento"}
        </Button>
      </div>
    </form>
  );
}
