"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { signAssignmentAction, type SignActionState } from "./actions";

const initialState: SignActionState = { error: null, success: false };

export function SignAssignmentForm({ assignmentId }: { assignmentId: string }) {
  const boundAction = signAssignmentAction.bind(null, assignmentId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  if (state.success) {
    return <p className="text-success text-sm font-medium">Recepción confirmada.</p>;
  }

  return (
    <form action={formAction} className="flex flex-col gap-2 sm:flex-row sm:items-end">
      <div className="flex flex-col gap-1">
        <Label htmlFor={`typedFullName-${assignmentId}`}>Escribe tu nombre completo para confirmar</Label>
        <Input id={`typedFullName-${assignmentId}`} name="typedFullName" required maxLength={200} placeholder="Nombre y apellidos" />
      </div>
      <Button type="submit" disabled={pending}>
        {pending ? "Confirmando…" : "Confirmar recepción"}
      </Button>
      {state.error && <p className="text-destructive text-sm">{state.error}</p>}
    </form>
  );
}
