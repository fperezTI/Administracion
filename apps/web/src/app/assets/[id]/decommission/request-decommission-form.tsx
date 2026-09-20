"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { requestDecommissionAction, type RequestDecommissionActionState } from "./actions";

const initialState: RequestDecommissionActionState = { error: null };

export function RequestDecommissionForm({ assetId }: { assetId: string }) {
  const boundAction = requestDecommissionAction.bind(null, assetId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="justification">Justificación</Label>
        <textarea
          id="justification"
          name="justification"
          required
          maxLength={1000}
          rows={4}
          placeholder="Motivo de la baja — se envía como evidencia junto con la solicitud de aprobación."
          className="rounded-lg border border-input bg-transparent px-2.5 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
        />
      </div>
      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}
      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Enviando…" : "Solicitar baja"}
        </Button>
      </div>
    </form>
  );
}
