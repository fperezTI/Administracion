"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { requestDisposalAction, type RequestDisposalActionState } from "./actions";

const initialState: RequestDisposalActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

export function RequestDisposalForm({ assetId }: { assetId: string }) {
  const boundAction = requestDisposalAction.bind(null, assetId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="targetStatus">Destino</Label>
        <select id="targetStatus" name="targetStatus" defaultValue="Sold" className={selectClassName} required>
          <option value="Sold">Venta</option>
          <option value="Donated">Donación</option>
          <option value="Destroyed">Destrucción</option>
        </select>
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="justification">Justificación</Label>
        <textarea
          id="justification"
          name="justification"
          required
          maxLength={1000}
          rows={4}
          className="rounded-lg border border-input bg-transparent px-2.5 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
        />
      </div>
      {state.error && <p className="text-destructive text-sm">{state.error}</p>}
      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Enviando…" : "Solicitar disposición"}
        </Button>
      </div>
    </form>
  );
}
