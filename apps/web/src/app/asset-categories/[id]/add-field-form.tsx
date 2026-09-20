"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Field } from "@/components/ui/field";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { CUSTOM_FIELD_DATA_TYPE_LABELS } from "@/lib/asset-labels";
import { addFieldAction, type AddFieldActionState } from "./actions";

const initialState: AddFieldActionState = { error: null };

export function AddFieldForm({ categoryId }: { categoryId: string }) {
  const boundAction = addFieldAction.bind(null, categoryId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-3 rounded-lg border p-4">
      <p className="text-sm font-medium">Agregar campo técnico</p>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <Field label="Nombre" htmlFor="name">
          <Input id="name" name="name" required maxLength={100} placeholder="p. ej. RAM" />
        </Field>
        <Field label="Código" htmlFor="code">
          <Input id="code" name="code" required maxLength={50} placeholder="p. ej. RAM" />
        </Field>
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <Field label="Tipo de dato" htmlFor="dataType">
          <Select name="dataType" defaultValue="Text">
            <SelectTrigger id="dataType" className="w-full">
              <SelectValue>{(value: keyof typeof CUSTOM_FIELD_DATA_TYPE_LABELS) => CUSTOM_FIELD_DATA_TYPE_LABELS[value]}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Text">Texto</SelectItem>
              <SelectItem value="Number">Número</SelectItem>
              <SelectItem value="Date">Fecha</SelectItem>
              <SelectItem value="Boolean">Sí/No</SelectItem>
              <SelectItem value="Select">Selección</SelectItem>
            </SelectContent>
          </Select>
        </Field>
        <div className="flex items-end gap-2 pb-1.5">
          <input id="isRequired" name="isRequired" type="checkbox" className="size-4" />
          <Label htmlFor="isRequired">Obligatorio</Label>
        </div>
      </div>
      <Field label="Opciones (solo para Selección, separadas por coma)" htmlFor="options">
        <Input id="options" name="options" placeholder="p. ej. 8GB, 16GB, 32GB" />
      </Field>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" size="sm" disabled={pending}>
          {pending ? "Agregando…" : "Agregar campo"}
        </Button>
      </div>
    </form>
  );
}
