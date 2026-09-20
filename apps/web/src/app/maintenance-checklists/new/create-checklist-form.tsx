"use client";

import { useActionState } from "react";
import type { AssetCategorySummary } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/ui/field";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createChecklistAction, type CreateChecklistActionState } from "./actions";

const initialState: CreateChecklistActionState = { error: null };

export function CreateChecklistForm({ categories }: { categories: AssetCategorySummary[] }) {
  const [state, formAction, pending] = useActionState(createChecklistAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Field label="Nombre" htmlFor="name">
          <Input id="name" name="name" required maxLength={200} placeholder="PM Laptop" />
        </Field>
        <Field label="Clave" htmlFor="key">
          <Input id="key" name="key" required maxLength={100} placeholder="pm-laptop" />
        </Field>
      </div>
      <Field label="Categoría de activo (opcional)" htmlFor="assetCategoryId">
        <Select name="assetCategoryId" defaultValue="">
          <SelectTrigger id="assetCategoryId" className="w-full">
            <SelectValue>
              {(value: string) => (value === "" ? "Cualquier categoría" : categories.find((c) => c.id === value)?.name)}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">Cualquier categoría</SelectItem>
            {categories.map((c) => (
              <SelectItem key={c.id} value={c.id}>
                {c.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>
      <Field label="Ítems (uno por línea)" htmlFor="items">
        <textarea
          id="items"
          name="items"
          required
          rows={8}
          placeholder={"Limpiar ventiladores\nActualizar BIOS\nRevisar batería"}
          className="rounded-lg border border-input bg-transparent px-2.5 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
        />
      </Field>
      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}
      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear checklist"}
        </Button>
      </div>
    </form>
  );
}
