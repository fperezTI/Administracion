"use client";

import { useActionState } from "react";
import type { AssetSummary } from "@/lib/api";
import { WARRANTY_TYPE_LABELS } from "@/lib/maintenance-labels";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/ui/field";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createWarrantyAction, type CreateWarrantyActionState } from "./actions";

const initialState: CreateWarrantyActionState = { error: null };

export function CreateWarrantyForm({ assets, companyId }: { assets: AssetSummary[]; companyId: string }) {
  const [state, formAction, pending] = useActionState(createWarrantyAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="companyId" value={companyId} />

      <Field label="Activo" htmlFor="assetId">
        <Select name="assetId" required>
          <SelectTrigger id="assetId" className="w-full">
            <SelectValue placeholder="Selecciona">
              {(value: string) => {
                const asset = assets.find((a) => a.id === value);
                return asset ? `${asset.internalFolio} — ${asset.brand} ${asset.model}` : null;
              }}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {assets.map((asset) => (
              <SelectItem key={asset.id} value={asset.id}>
                {asset.internalFolio} — {asset.brand} {asset.model}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>

      <Field label="Tipo" htmlFor="type">
        <Select name="type" required>
          <SelectTrigger id="type" className="w-full">
            <SelectValue placeholder="Selecciona">
              {(value: keyof typeof WARRANTY_TYPE_LABELS) => WARRANTY_TYPE_LABELS[value]}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {Object.entries(WARRANTY_TYPE_LABELS).map(([value, label]) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>

      <Field label="Proveedor" htmlFor="provider">
        <Input id="provider" name="provider" required maxLength={200} placeholder="Dell ProSupport" />
      </Field>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Field label="Inicio" htmlFor="startDate">
          <Input id="startDate" name="startDate" type="date" required />
        </Field>
        <Field label="Fin" htmlFor="endDate">
          <Input id="endDate" name="endDate" type="date" required />
        </Field>
      </div>

      <Field label="Términos (opcional)" htmlFor="terms">
        <Input id="terms" name="terms" maxLength={2000} placeholder="3 años, incluye partes y mano de obra" />
      </Field>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Registrar garantía"}
        </Button>
      </div>
    </form>
  );
}
