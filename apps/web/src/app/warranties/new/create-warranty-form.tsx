"use client";

import { useActionState } from "react";
import type { AssetSummary } from "@/lib/api";
import { WARRANTY_TYPE_LABELS } from "@/lib/maintenance-labels";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createWarrantyAction, type CreateWarrantyActionState } from "./actions";

const initialState: CreateWarrantyActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

export function CreateWarrantyForm({ assets, companyId }: { assets: AssetSummary[]; companyId: string }) {
  const [state, formAction, pending] = useActionState(createWarrantyAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="companyId" value={companyId} />

      <div className="flex flex-col gap-1">
        <Label htmlFor="assetId">Activo</Label>
        <select id="assetId" name="assetId" className={selectClassName} required defaultValue="">
          <option value="" disabled>
            Selecciona
          </option>
          {assets.map((asset) => (
            <option key={asset.id} value={asset.id}>
              {asset.internalFolio} — {asset.brand} {asset.model}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="type">Tipo</Label>
        <select id="type" name="type" className={selectClassName} required defaultValue="">
          <option value="" disabled>
            Selecciona
          </option>
          {Object.entries(WARRANTY_TYPE_LABELS).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="provider">Proveedor</Label>
        <Input id="provider" name="provider" required maxLength={200} placeholder="Dell ProSupport" />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1">
          <Label htmlFor="startDate">Inicio</Label>
          <Input id="startDate" name="startDate" type="date" required />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="endDate">Fin</Label>
          <Input id="endDate" name="endDate" type="date" required />
        </div>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="terms">Términos (opcional)</Label>
        <Input id="terms" name="terms" maxLength={2000} placeholder="3 años, incluye partes y mano de obra" />
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Registrar garantía"}
        </Button>
      </div>
    </form>
  );
}
