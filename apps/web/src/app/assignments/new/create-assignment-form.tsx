"use client";

import { useActionState } from "react";
import type { AssetSummary, OrgUnitNode, UserSummary } from "@/lib/api";
import { flattenOrgUnitTree } from "@/lib/org-unit-tree";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createAssignmentAction, type CreateAssignmentActionState } from "./actions";

const initialState: CreateAssignmentActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

export function CreateAssignmentForm({
  assets,
  users,
  orgUnits,
}: {
  assets: AssetSummary[];
  users: UserSummary[];
  orgUnits: OrgUnitNode[];
}) {
  const [state, formAction, pending] = useActionState(createAssignmentAction, initialState);
  const orgUnitOptions = flattenOrgUnitTree(orgUnits);

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
          <p className="text-muted-foreground text-xs">No hay activos en almacén disponibles para asignar.</p>
        )}
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="assignedToUserId">Destinatario</Label>
        <select id="assignedToUserId" name="assignedToUserId" className={selectClassName} required>
          <option value="" disabled defaultValue="">
            Selecciona
          </option>
          {users.map((user) => (
            <option key={user.id} value={user.id}>
              {user.displayName} — {user.email}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="orgUnitId">Unidad organizacional (opcional)</Label>
        <select id="orgUnitId" name="orgUnitId" defaultValue="" className={selectClassName}>
          <option value="">Sin asignar</option>
          {orgUnitOptions.map((option) => (
            <option key={option.id} value={option.id}>
              {option.label}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="notes">Notas (opcional)</Label>
        <Input id="notes" name="notes" maxLength={500} />
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear asignación"}
        </Button>
      </div>
    </form>
  );
}
