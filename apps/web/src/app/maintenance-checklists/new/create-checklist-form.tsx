"use client";

import { useActionState } from "react";
import type { AssetCategorySummary } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createChecklistAction, type CreateChecklistActionState } from "./actions";

const initialState: CreateChecklistActionState = { error: null };

export function CreateChecklistForm({ categories }: { categories: AssetCategorySummary[] }) {
  const [state, formAction, pending] = useActionState(createChecklistAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1">
          <Label htmlFor="name">Nombre</Label>
          <Input id="name" name="name" required maxLength={200} placeholder="PM Laptop" />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="key">Clave</Label>
          <Input id="key" name="key" required maxLength={100} placeholder="pm-laptop" />
        </div>
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="assetCategoryId">Categoría de activo (opcional)</Label>
        <select
          id="assetCategoryId"
          name="assetCategoryId"
          className="border-input bg-transparent focus-visible:border-ring focus-visible:ring-ring/50 h-9 rounded-lg border px-2.5 text-sm outline-none focus-visible:ring-3 dark:bg-input/30"
          defaultValue=""
        >
          <option value="">Cualquier categoría</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="items">Ítems (uno por línea)</Label>
        <textarea
          id="items"
          name="items"
          required
          rows={8}
          placeholder={"Limpiar ventiladores\nActualizar BIOS\nRevisar batería"}
          className="rounded-lg border border-input bg-transparent px-2.5 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
        />
      </div>
      {state.error && <p className="text-destructive text-sm">{state.error}</p>}
      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear checklist"}
        </Button>
      </div>
    </form>
  );
}
