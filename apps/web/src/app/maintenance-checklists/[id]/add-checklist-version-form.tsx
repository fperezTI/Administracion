"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { addChecklistVersionAction, type AddChecklistVersionActionState } from "./actions";

const initialState: AddChecklistVersionActionState = { error: null, success: false };

export function AddChecklistVersionForm({ checklistDefinitionId }: { checklistDefinitionId: string }) {
  const boundAction = addChecklistVersionAction.bind(null, checklistDefinitionId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-3">
      <textarea
        name="items"
        required
        rows={8}
        placeholder={"Un ítem por línea"}
        className="rounded-lg border border-input bg-transparent px-2.5 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
      />
      <div className="flex items-center justify-between">
        <p className="text-sm" role="status">
          {state.error && <span className="text-destructive">{state.error}</span>}
          {!state.error && state.success && <span className="text-success">Versión agregada.</span>}
        </p>
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? "Guardando…" : "Agregar versión"}
        </Button>
      </div>
    </form>
  );
}
