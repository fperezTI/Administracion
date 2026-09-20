"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createRoleAction, type CreateRoleActionState } from "./actions";

const initialState: CreateRoleActionState = { error: null };

export function CreateRoleForm() {
  const [state, formAction, pending] = useActionState(createRoleAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="name">Nombre</Label>
        <Input id="name" name="name" required maxLength={100} placeholder="p. ej. Técnico de soporte" />
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="description">Descripción</Label>
        <Input id="description" name="description" maxLength={500} />
      </div>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear rol"}
        </Button>
      </div>
    </form>
  );
}
