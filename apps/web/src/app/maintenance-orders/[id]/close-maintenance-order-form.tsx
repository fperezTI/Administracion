"use client";

import { useActionState } from "react";
import type { MaintenanceOrderChecklistResultInfo } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { closeMaintenanceOrderAction, type CloseMaintenanceOrderActionState } from "./actions";

const initialState: CloseMaintenanceOrderActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

export function CloseMaintenanceOrderForm({
  maintenanceOrderId,
  checklistResults,
}: {
  maintenanceOrderId: string;
  checklistResults: MaintenanceOrderChecklistResultInfo[];
}) {
  const boundAction = closeMaintenanceOrderAction.bind(null, maintenanceOrderId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="itemIndexes" value={checklistResults.map((r) => r.itemIndex).join(",")} />

      {checklistResults.length > 0 && (
        <div className="flex flex-col gap-2">
          <Label>Checklist</Label>
          {checklistResults.map((item) => (
            <div key={item.itemIndex} className="flex items-start gap-2">
              <input
                type="checkbox"
                id={`item-${item.itemIndex}-completed`}
                name={`item-${item.itemIndex}-completed`}
                defaultChecked={item.isCompleted}
                className="border-input mt-0.5 size-4 rounded"
              />
              <div className="flex flex-1 flex-col gap-1">
                <label htmlFor={`item-${item.itemIndex}-completed`} className="text-sm">
                  {item.itemText}
                </label>
                <input
                  type="text"
                  name={`item-${item.itemIndex}-notes`}
                  defaultValue={item.notes ?? ""}
                  placeholder="Notas (opcional)"
                  maxLength={500}
                  className="border-input bg-transparent focus-visible:border-ring focus-visible:ring-ring/50 h-7 rounded-md border px-2 text-xs outline-none focus-visible:ring-3 dark:bg-input/30"
                />
              </div>
            </div>
          ))}
        </div>
      )}

      <div className="flex flex-col gap-1">
        <Label htmlFor="resultStatus">Resultado</Label>
        <select id="resultStatus" name="resultStatus" className={selectClassName} required defaultValue="">
          <option value="" disabled>
            Selecciona
          </option>
          <option value="InWarehouse">Reparado — vuelve a almacén</option>
          <option value="Damaged">No se pudo reparar — queda dañado</option>
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="resultNotes">Descripción del resultado (evidencia)</Label>
        <textarea
          id="resultNotes"
          name="resultNotes"
          required
          maxLength={2000}
          rows={4}
          className="rounded-lg border border-input bg-transparent px-2.5 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
        />
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Cerrando…" : "Cerrar orden"}
        </Button>
      </div>
    </form>
  );
}
