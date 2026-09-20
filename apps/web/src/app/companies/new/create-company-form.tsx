"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createCompanyAction, type CreateCompanyActionState } from "./actions";

const initialState: CreateCompanyActionState = { error: null };

export function CreateCompanyForm() {
  const [state, formAction, pending] = useActionState(createCompanyAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="tradeName">Nombre comercial</Label>
        <Input id="tradeName" name="tradeName" required maxLength={200} />
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="legalName">Razón social</Label>
        <Input id="legalName" name="legalName" required maxLength={200} />
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1">
          <Label htmlFor="taxId">RFC / Identificación fiscal</Label>
          <Input id="taxId" name="taxId" required maxLength={50} />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="baseCurrency">Moneda base (ISO 4217)</Label>
          <Input id="baseCurrency" name="baseCurrency" required maxLength={3} defaultValue="MXN" />
        </div>
      </div>
      <div className="flex flex-col gap-1">
        <Label htmlFor="timeZone">Zona horaria (IANA)</Label>
        <Input id="timeZone" name="timeZone" required maxLength={100} defaultValue="America/Mexico_City" />
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear empresa"}
        </Button>
      </div>
    </form>
  );
}
