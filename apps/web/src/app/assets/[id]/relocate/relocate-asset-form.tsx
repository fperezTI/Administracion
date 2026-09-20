"use client";

import { useActionState } from "react";
import type { OrgUnitNode } from "@/lib/api";
import { flattenOrgUnitTree } from "@/lib/org-unit-tree";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { relocateAssetAction, type RelocateActionState } from "./actions";

const initialState: RelocateActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

export function RelocateAssetForm({
  assetId,
  orgUnits,
  currentOrgUnitId,
}: {
  assetId: string;
  orgUnits: OrgUnitNode[];
  currentOrgUnitId: string | null;
}) {
  const boundAction = relocateAssetAction.bind(null, assetId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);
  const orgUnitOptions = flattenOrgUnitTree(orgUnits);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="newOrgUnitId">Nueva ubicación</Label>
        <select id="newOrgUnitId" name="newOrgUnitId" defaultValue={currentOrgUnitId ?? ""} className={selectClassName}>
          <option value="">Sin asignar</option>
          {orgUnitOptions.map((option) => (
            <option key={option.id} value={option.id}>
              {option.label}
            </option>
          ))}
        </select>
        {orgUnitOptions.length === 0 && (
          <p className="text-muted-foreground text-xs">Esta empresa todavía no tiene estructura organizacional configurada.</p>
        )}
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="notes">Motivo (opcional)</Label>
        <Input id="notes" name="notes" maxLength={500} />
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Reubicar"}
        </Button>
      </div>
    </form>
  );
}
