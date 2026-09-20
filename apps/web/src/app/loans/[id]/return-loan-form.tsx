"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { returnLoanAction, type ReturnLoanActionState } from "./actions";

const initialState: ReturnLoanActionState = { error: null, success: false };

export function ReturnLoanForm({ loanId }: { loanId: string }) {
  const boundAction = returnLoanAction.bind(null, loanId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-3 rounded-lg border p-4">
      <p className="text-sm font-medium">Registrar devolución</p>
      <div className="flex flex-col gap-1">
        <Label htmlFor="notes">Notas (opcional)</Label>
        <Input id="notes" name="notes" maxLength={500} />
      </div>
      <div className="flex items-center justify-between">
        <p className="text-sm" role="alert">{state.error && <span className="text-destructive">{state.error}</span>}</p>
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Registrar devolución"}
        </Button>
      </div>
    </form>
  );
}
