"use client";

import { useActionState } from "react";
import type { AssetSummary } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { installSparePartAction, type SparePartActionState } from "./actions";

const initialState: SparePartActionState = { error: null };

export function InstallSparePartForm({ sparePartId, assets }: { sparePartId: string; assets: AssetSummary[] }) {
  const boundAction = installSparePartAction.bind(null, sparePartId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex items-end gap-2">
      <div className="flex flex-1 flex-col gap-1">
        <select
          name="assetId"
          required
          defaultValue=""
          className="border-input bg-transparent focus-visible:border-ring focus-visible:ring-ring/50 h-8 rounded-lg border px-2.5 text-sm outline-none focus-visible:ring-3 dark:bg-input/30"
        >
          <option value="" disabled>
            Selecciona un activo
          </option>
          {assets.map((asset) => (
            <option key={asset.id} value={asset.id}>
              {asset.internalFolio} — {asset.brand} {asset.model}
            </option>
          ))}
        </select>
        {state.error && <p className="text-destructive text-xs">{state.error}</p>}
      </div>
      <Button type="submit" size="sm" disabled={pending}>
        {pending ? "Instalando…" : "Instalar"}
      </Button>
    </form>
  );
}
