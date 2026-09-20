"use client";

import { useActionState } from "react";
import type { OrgUnitNode } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { uninstallSparePartAction, type SparePartActionState } from "./actions";

const initialState: SparePartActionState = { error: null };

export function UninstallSparePartForm({ sparePartId, warehouses }: { sparePartId: string; warehouses: OrgUnitNode[] }) {
  const boundAction = uninstallSparePartAction.bind(null, sparePartId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex items-end gap-2">
      <div className="flex flex-1 flex-col gap-1">
        <select
          name="warehouseOrgUnitId"
          required
          defaultValue=""
          className="border-input bg-transparent focus-visible:border-ring focus-visible:ring-ring/50 h-8 rounded-lg border px-2.5 text-sm outline-none focus-visible:ring-3 dark:bg-input/30"
        >
          <option value="" disabled>
            Selecciona un almacén de destino
          </option>
          {warehouses.map((w) => (
            <option key={w.id} value={w.id}>
              {w.name}
            </option>
          ))}
        </select>
        {state.error && <p className="text-destructive text-xs">{state.error}</p>}
      </div>
      <Button type="submit" size="sm" variant="outline" disabled={pending}>
        {pending ? "Retirando…" : "Retirar"}
      </Button>
    </form>
  );
}
