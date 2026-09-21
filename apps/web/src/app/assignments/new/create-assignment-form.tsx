"use client";

import { useState } from "react";
import { useActionState } from "react";
import type { AssetSummary, OrgUnitNode, UserSummary } from "@/lib/api";
import { flattenOrgUnitTree } from "@/lib/org-unit-tree";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createAssignmentAction, type CreateAssignmentActionState } from "./actions";

const initialState: CreateAssignmentActionState = { error: null };

export function CreateAssignmentForm({
  assets,
  users,
  orgUnits,
  defaultAssetId,
}: {
  assets: AssetSummary[];
  users: UserSummary[];
  orgUnits: OrgUnitNode[];
  defaultAssetId?: string | null;
}) {
  const [state, formAction, pending] = useActionState(createAssignmentAction, initialState);
  const orgUnitOptions = flattenOrgUnitTree(orgUnits);
  const [selectedAssetId, setSelectedAssetId] = useState(defaultAssetId ?? "");
  const availableAccessories = assets.filter((a) => a.accessoryOfAssetId === selectedAssetId);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="assetId">Activo (debe estar en almacén)</Label>
        <Select
          name="assetId"
          required
          value={selectedAssetId}
          onValueChange={(value) => setSelectedAssetId(value ?? "")}
        >
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
          <p className="text-muted-foreground text-xs">No hay activos en almacén disponibles para asignar.</p>
        )}
      </div>

      {availableAccessories.length > 0 && (
        <div className="flex flex-col gap-2">
          <Label>Accesorios a incluir</Label>
          {availableAccessories.map((accessory) => (
            <label key={accessory.id} className="flex items-center gap-2 text-sm">
              <input type="checkbox" name="accessoryAssetIds" value={accessory.id} defaultChecked className="size-4" />
              {accessory.internalFolio} — {accessory.brand} {accessory.model}
            </label>
          ))}
        </div>
      )}

      <div className="flex flex-col gap-1">
        <Label htmlFor="assignedToUserId">Destinatario</Label>
        <Select name="assignedToUserId" required>
          <SelectTrigger id="assignedToUserId" className="w-full">
            <SelectValue placeholder="Selecciona">
              {(value: string) => {
                const user = users.find((u) => u.id === value);
                return user ? `${user.displayName} — ${user.email}` : null;
              }}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {users.map((user) => (
              <SelectItem key={user.id} value={user.id}>
                {user.displayName} — {user.email}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="orgUnitId">Unidad organizacional (opcional)</Label>
        <Select name="orgUnitId" defaultValue="">
          <SelectTrigger id="orgUnitId" className="w-full">
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
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="notes">Notas (opcional)</Label>
        <Input id="notes" name="notes" maxLength={500} />
      </div>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear asignación"}
        </Button>
      </div>
    </form>
  );
}
