"use client";

import { useActionState, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createCompanyAction, type CreateCompanyActionState } from "./actions";

const initialState: CreateCompanyActionState = { error: null };

/** Toda zona horaria IANA que el motor conoce — ni una lista propia que mantener ni riesgo de
 * proponer un valor que la base de tz no reconozca. */
const TIME_ZONES = Intl.supportedValuesOf("timeZone");

export function CreateCompanyForm() {
  const [state, formAction, pending] = useActionState(createCompanyAction, initialState);
  const [timeZone, setTimeZone] = useState("America/Mexico_City");

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
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
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
        <Select name="timeZone" value={timeZone} onValueChange={(value) => setTimeZone(value ?? "")} required>
          <SelectTrigger id="timeZone" className="w-full">
            <SelectValue>{(value: string) => value}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            {TIME_ZONES.map((zone) => (
              <SelectItem key={zone} value={zone}>
                {zone}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear empresa"}
        </Button>
      </div>
    </form>
  );
}
