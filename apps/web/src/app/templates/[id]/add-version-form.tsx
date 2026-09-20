"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { addVersionAction, type AddVersionActionState } from "./actions";

const initialState: AddVersionActionState = { error: null, success: false };

export function AddVersionForm({ templateId }: { templateId: string }) {
  const boundAction = addVersionAction.bind(null, templateId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-3">
      <textarea
        name="content"
        required
        maxLength={10000}
        rows={8}
        placeholder="Contenido de la nueva versión"
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
