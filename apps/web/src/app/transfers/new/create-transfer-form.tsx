"use client";

import { useActionState } from "react";
import type { AssetSummary, CompanySummary } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { requestTransferAction, type RequestTransferActionState } from "./actions";

const initialState: RequestTransferActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

export function CreateTransferForm({ assets, companies }: { assets: AssetSummary[]; companies: CompanySummary[] }) {
  const [state, formAction, pending] = useActionState(requestTransferAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="assetId">Activo (debe estar en almacén)</Label>
        <select id="assetId" name="assetId" className={selectClassName} required>
          <option value="" disabled defaultValue="">
            Selecciona
          </option>
          {assets.map((asset) => (
            <option key={asset.id} value={asset.id}>
              {asset.internalFolio} — {asset.brand} {asset.model}
            </option>
          ))}
        </select>
        {assets.length === 0 && (
          <p className="text-muted-foreground text-xs">No hay activos en almacén disponibles para transferir.</p>
        )}
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="toCompanyId">Empresa destino</Label>
        <select id="toCompanyId" name="toCompanyId" className={selectClassName} required>
          <option value="" disabled defaultValue="">
            Selecciona
          </option>
          {companies.map((company) => (
            <option key={company.id} value={company.id}>
              {company.tradeName}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="notes">Justificación (opcional)</Label>
        <Input id="notes" name="notes" maxLength={1000} />
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Enviando…" : "Solicitar transferencia"}
        </Button>
      </div>
    </form>
  );
}
