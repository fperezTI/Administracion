"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { addFieldAction, type AddFieldActionState } from "./actions";

const initialState: AddFieldActionState = { error: null };

export function AddFieldForm({ categoryId }: { categoryId: string }) {
  const boundAction = addFieldAction.bind(null, categoryId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-3 rounded-lg border p-4">
      <p className="text-sm font-medium">Agregar campo técnico</p>
      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1">
          <Label htmlFor="name">Nombre</Label>
          <Input id="name" name="name" required maxLength={100} placeholder="p. ej. RAM" />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="code">Código</Label>
          <Input id="code" name="code" required maxLength={50} placeholder="p. ej. RAM" />
        </div>
      </div>
      <div className="grid grid-cols-2 gap-3">
        <div className="flex flex-col gap-1">
          <Label htmlFor="dataType">Tipo de dato</Label>
          <select
            id="dataType"
            name="dataType"
            defaultValue="Text"
            className="h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none dark:bg-input/30"
          >
            <option value="Text">Texto</option>
            <option value="Number">Número</option>
            <option value="Date">Fecha</option>
            <option value="Boolean">Sí/No</option>
            <option value="Select">Selección</option>
          </select>
        </div>
        <div className="flex items-end gap-2 pb-1.5">
          <input id="isRequired" name="isRequired" type="checkbox" className="size-4" />
          <Label htmlFor="isRequired">Obligatorio</Label>
        </div>
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="options">Opciones (solo para Selección, separadas por coma)</Label>
        <Input id="options" name="options" placeholder="p. ej. 8GB, 16GB, 32GB" />
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? "Agregando…" : "Agregar campo"}
        </Button>
      </div>
    </form>
  );
}
