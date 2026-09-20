"use client";

import { useActionState } from "react";
import type { AssetSummary, MaintenanceChecklistDefinitionSummary } from "@/lib/api";
import { MAINTENANCE_ORDER_TYPE_LABELS } from "@/lib/maintenance-labels";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { openMaintenanceOrderAction, type OpenMaintenanceOrderActionState } from "./actions";

const initialState: OpenMaintenanceOrderActionState = { error: null };

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
        <Select name="assetId" required defaultValue={defaultAssetId ?? undefined}>
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
          <p className="text-muted-foreground text-xs">No hay activos elegibles para mantenimiento.</p>
        )}
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="type">Tipo</Label>
        <Select name="type" required>
          <SelectTrigger id="type" className="w-full">
            <SelectValue placeholder="Selecciona">
              {(value: keyof typeof MAINTENANCE_ORDER_TYPE_LABELS) => MAINTENANCE_ORDER_TYPE_LABELS[value]}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {Object.entries(MAINTENANCE_ORDER_TYPE_LABELS).map(([value, label]) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="checklistDefinitionId">Checklist (opcional)</Label>
        <Select name="checklistDefinitionId" defaultValue="">
          <SelectTrigger id="checklistDefinitionId" className="w-full">
            <SelectValue>
              {(value: string) => {
                if (value === "") return "Sin checklist";
                const checklist = checklists.find((c) => c.id === value);
                return checklist ? `${checklist.name} (v${checklist.latestVersionNumber})` : null;
              }}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">Sin checklist</SelectItem>
            {checklists.map((c) => (
              <SelectItem key={c.id} value={c.id}>
                {c.name} (v{c.latestVersionNumber})
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
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

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Enviando…" : "Abrir orden"}
        </Button>
      </div>
    </form>
  );
}
