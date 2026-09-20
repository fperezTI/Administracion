"use client";

import { useActionState } from "react";
import type { OrgUnitNode } from "@/lib/api";
import { flattenOrgUnitTree } from "@/lib/org-unit-tree";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { relocateAssetAction, type RelocateActionState } from "./actions";

const initialState: RelocateActionState = { error: null };

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
        <Select name="newOrgUnitId" defaultValue={currentOrgUnitId ?? ""}>
          <SelectTrigger id="newOrgUnitId" className="w-full">
            <SelectValue>
              {(value: string) => (value === "" ? "Sin asignar" : orgUnitOptions.find((o) => o.id === value)?.label)}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">Sin asignar</SelectItem>
            {orgUnitOptions.map((option) => (
              <SelectItem key={option.id} value={option.id}>
                {option.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {orgUnitOptions.length === 0 && (
          <p className="text-muted-foreground text-xs">Esta empresa todavía no tiene estructura organizacional configurada.</p>
        )}
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="notes">Motivo (opcional)</Label>
        <Input id="notes" name="notes" maxLength={500} />
      </div>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Reubicar"}
        </Button>
      </div>
    </form>
  );
}
