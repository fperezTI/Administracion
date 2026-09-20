"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createTemplateAction, type CreateTemplateActionState } from "./actions";

const initialState: CreateTemplateActionState = { error: null };

export function CreateTemplateForm() {
  const [state, formAction, pending] = useActionState(createTemplateAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="flex flex-col gap-1">
          <Label htmlFor="name">Nombre</Label>
          <Input id="name" name="name" required maxLength={200} placeholder="Carta resguardo" />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="key">Clave</Label>
          <Input id="key" name="key" required maxLength={100} placeholder="asset.custody-letter" />
        </div>
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="initialContent">Contenido</Label>
        <textarea
          id="initialContent"
          name="initialContent"
          required
          maxLength={10000}
          rows={10}
          className="rounded-lg border border-input bg-transparent px-2.5 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
        />
      </div>
      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}
      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear plantilla"}
        </Button>
      </div>
    </form>
  );
}
