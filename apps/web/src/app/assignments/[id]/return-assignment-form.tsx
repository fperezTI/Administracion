"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { returnAssignmentAction, type AssignmentActionState } from "./actions";

const initialState: AssignmentActionState = { error: null, success: false };

export function ReturnAssignmentForm({ assignmentId }: { assignmentId: string }) {
  const boundAction = returnAssignmentAction.bind(null, assignmentId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-3 rounded-lg border p-4">
      <p className="text-sm font-medium">Registrar devolución</p>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div className="flex flex-col gap-1">
          <Label htmlFor="typedFullName">Tu nombre completo (confirma la recepción de vuelta)</Label>
          <Input id="typedFullName" name="typedFullName" required maxLength={200} />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="notes">Notas (opcional)</Label>
          <Input id="notes" name="notes" maxLength={500} />
        </div>
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
