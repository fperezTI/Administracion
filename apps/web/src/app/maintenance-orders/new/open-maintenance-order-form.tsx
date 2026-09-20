"use client";

import { useActionState } from "react";
import type { AssetSummary, MaintenanceChecklistDefinitionSummary } from "@/lib/api";
import { MAINTENANCE_ORDER_TYPE_LABELS } from "@/lib/maintenance-labels";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { openMaintenanceOrderAction, type OpenMaintenanceOrderActionState } from "./actions";

const initialState: OpenMaintenanceOrderActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

export function OpenMaintenanceOrderForm({
  assets,
  checklists,
  defaultAssetId,
}: {
  assets: AssetSummary[];
  checklists: MaintenanceChecklistDefinitionSummary[];
  defaultAssetId: string | null;
}) {
  const [state, formAction, pending] = useActionState(openMaintenanceOrderAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="assetId">Activo (en almacén o asignado)</Label>
        <select id="assetId" name="assetId" className={selectClassName} required defaultValue={defaultAssetId ?? ""}>
          <option value="" disabled>
            Selecciona
          </option>
          {assets.map((asset) => (
            <option key={asset.id} value={asset.id}>
              {asset.internalFolio} — {asset.brand} {asset.model}
            </option>
          ))}
        </select>
        {assets.length === 0 && (
          <p className="text-muted-foreground text-xs">No hay activos elegibles para mantenimiento.</p>
        )}
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="type">Tipo</Label>
        <select id="type" name="type" className={selectClassName} required defaultValue="">
          <option value="" disabled>
            Selecciona
          </option>
          {Object.entries(MAINTENANCE_ORDER_TYPE_LABELS).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="checklistDefinitionId">Checklist (opcional)</Label>
        <select id="checklistDefinitionId" name="checklistDefinitionId" className={selectClassName} defaultValue="">
          <option value="">Sin checklist</option>
          {checklists.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name} (v{c.latestVersionNumber})
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="description">Descripción</Label>
        <textarea
          id="description"
          name="description"
          required
          maxLength={1000}
          rows={4}
          placeholder="Motivo de la orden de mantenimiento"
          className="rounded-lg border border-input bg-transparent px-2.5 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
        />
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Enviando…" : "Abrir orden"}
        </Button>
      </div>
    </form>
  );
}
