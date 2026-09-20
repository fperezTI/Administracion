"use client";

import { useActionState } from "react";
import type { AssetSummary, CompanySummary } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { requestTransferAction, type RequestTransferActionState } from "./actions";

const initialState: RequestTransferActionState = { error: null };

export function CreateTransferForm({ assets, companies }: { assets: AssetSummary[]; companies: CompanySummary[] }) {
  const [state, formAction, pending] = useActionState(requestTransferAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="assetId">Activo (debe estar en almacén)</Label>
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
        {assets.length === 0 && (
          <p className="text-muted-foreground text-xs">No hay activos en almacén disponibles para transferir.</p>
        )}
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="toCompanyId">Empresa destino</Label>
        <Select name="toCompanyId" required>
          <SelectTrigger id="toCompanyId" className="w-full">
            <SelectValue placeholder="Selecciona">{(value: string) => companies.find((c) => c.id === value)?.tradeName}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            {companies.map((company) => (
              <SelectItem key={company.id} value={company.id}>
                {company.tradeName}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="notes">Justificación (opcional)</Label>
        <Input id="notes" name="notes" maxLength={1000} />
      </div>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Enviando…" : "Solicitar transferencia"}
        </Button>
      </div>
    </form>
  );
}
