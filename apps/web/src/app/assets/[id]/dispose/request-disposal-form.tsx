"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Field } from "@/components/ui/field";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { requestDisposalAction, type RequestDisposalActionState } from "./actions";

const initialState: RequestDisposalActionState = { error: null };

export function RequestDisposalForm({ assetId }: { assetId: string }) {
  const boundAction = requestDisposalAction.bind(null, assetId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <Field label="Destino" htmlFor="targetStatus">
        <Select name="targetStatus" defaultValue="Sold" required>
          <SelectTrigger id="targetStatus" className="w-full">
            <SelectValue>
              {(value: "Sold" | "Donated" | "Destroyed") =>
                ({ Sold: "Venta", Donated: "Donación", Destroyed: "Destrucción" })[value]
              }
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Sold">Venta</SelectItem>
            <SelectItem value="Donated">Donación</SelectItem>
            <SelectItem value="Destroyed">Destrucción</SelectItem>
          </SelectContent>
        </Select>
      </Field>
      <Field label="Justificación" htmlFor="justification">
        <textarea
          id="justification"
          name="justification"
          required
          maxLength={1000}
          rows={4}
          className="rounded-lg border border-input bg-transparent px-2.5 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
        />
      </Field>
      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}
      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Enviando…" : "Solicitar disposición"}
        </Button>
      </div>
    </form>
  );
}
