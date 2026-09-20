"use client";

import { useActionState } from "react";
import type { OrgUnitNode } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { uninstallSparePartAction, type SparePartActionState } from "./actions";

const initialState: SparePartActionState = { error: null };

export function UninstallSparePartForm({ sparePartId, warehouses }: { sparePartId: string; warehouses: OrgUnitNode[] }) {
  const boundAction = uninstallSparePartAction.bind(null, sparePartId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex items-end gap-2">
      <div className="flex flex-1 flex-col gap-1">
        <Select name="warehouseOrgUnitId" required>
          <SelectTrigger className="w-full">
            <SelectValue placeholder="Selecciona un almacén de destino">
              {(value: string) => warehouses.find((w) => w.id === value)?.name}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {warehouses.map((w) => (
              <SelectItem key={w.id} value={w.id}>
                {w.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {state.error && <p className="text-destructive text-xs" role="alert">{state.error}</p>}
      </div>
      <Button type="submit" size="sm" variant="outline" disabled={pending}>
        {pending ? "Retirando…" : "Retirar"}
      </Button>
    </form>
  );
}
