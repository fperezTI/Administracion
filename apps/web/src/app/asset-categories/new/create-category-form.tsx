"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createCategoryAction, type CreateCategoryActionState } from "./actions";

const initialState: CreateCategoryActionState = { error: null };

export function CreateCategoryForm() {
  const [state, formAction, pending] = useActionState(createCategoryAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="name">Nombre</Label>
        <Input id="name" name="name" required maxLength={100} placeholder="p. ej. Tablets" />
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="code">Código</Label>
        <Input id="code" name="code" required maxLength={50} placeholder="p. ej. TABLET" />
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="defaultIdentificationTechnology">Tecnología de identificación por defecto</Label>
        <select
          id="defaultIdentificationTechnology"
          name="defaultIdentificationTechnology"
          defaultValue="Qr"
          className="h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none dark:bg-input/30"
        >
          <option value="Qr">Código QR</option>
          <option value="Barcode">Código de barras</option>
          <option value="QrAndBarcode">QR y código de barras</option>
          <option value="Nfc">NFC</option>
          <option value="Rfid">RFID</option>
        </select>
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear categoría"}
        </Button>
      </div>
    </form>
  );
}
