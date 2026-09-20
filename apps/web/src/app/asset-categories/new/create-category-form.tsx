"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/ui/field";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { IDENTIFICATION_TECHNOLOGY_LABELS } from "@/lib/asset-labels";
import { createCategoryAction, type CreateCategoryActionState } from "./actions";

const initialState: CreateCategoryActionState = { error: null };

export function CreateCategoryForm() {
  const [state, formAction, pending] = useActionState(createCategoryAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <Field label="Nombre" htmlFor="name">
        <Input id="name" name="name" required maxLength={100} placeholder="p. ej. Tablets" />
      </Field>
      <Field label="Código" htmlFor="code">
        <Input id="code" name="code" required maxLength={50} placeholder="p. ej. TABLET" />
      </Field>
      <Field label="Tecnología de identificación por defecto" htmlFor="defaultIdentificationTechnology">
        <Select name="defaultIdentificationTechnology" defaultValue="Qr">
          <SelectTrigger id="defaultIdentificationTechnology" className="w-full">
            <SelectValue>{(value: keyof typeof IDENTIFICATION_TECHNOLOGY_LABELS) => IDENTIFICATION_TECHNOLOGY_LABELS[value]}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Qr">Código QR</SelectItem>
            <SelectItem value="Barcode">Código de barras</SelectItem>
            <SelectItem value="QrAndBarcode">QR y código de barras</SelectItem>
            <SelectItem value="Nfc">NFC</SelectItem>
            <SelectItem value="Rfid">RFID</SelectItem>
          </SelectContent>
        </Select>
      </Field>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear categoría"}
        </Button>
      </div>
    </form>
  );
}
