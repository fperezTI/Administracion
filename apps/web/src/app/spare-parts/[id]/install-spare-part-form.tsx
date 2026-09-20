"use client";

import { useActionState } from "react";
import type { AssetSummary } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { installSparePartAction, type SparePartActionState } from "./actions";

const initialState: SparePartActionState = { error: null };

export function InstallSparePartForm({ sparePartId, assets }: { sparePartId: string; assets: AssetSummary[] }) {
  const boundAction = installSparePartAction.bind(null, sparePartId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex items-end gap-2">
      <div className="flex flex-1 flex-col gap-1">
        <Select name="assetId" required>
          <SelectTrigger className="w-full">
            <SelectValue placeholder="Selecciona un activo">
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
        {state.error && <p className="text-destructive text-xs" role="alert">{state.error}</p>}
      </div>
      <Button type="submit" size="sm" disabled={pending}>
        {pending ? "Instalando…" : "Instalar"}
      </Button>
    </form>
  );
}
