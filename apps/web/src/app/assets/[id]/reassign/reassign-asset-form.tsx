"use client";

import { useActionState } from "react";
import type { AssetAccessorySummary, OrgUnitNode, UserSummary } from "@/lib/api";
import { flattenOrgUnitTree } from "@/lib/org-unit-tree";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { reassignAssetAction, type ReassignActionState } from "./actions";

const initialState: ReassignActionState = { error: null };

export function ReassignAssetForm({
  assetId,
  users,
  orgUnits,
  accessories,
}: {
  assetId: string;
  users: UserSummary[];
  orgUnits: OrgUnitNode[];
  accessories: AssetAccessorySummary[];
}) {
  const boundAction = reassignAssetAction.bind(null, assetId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);
  const orgUnitOptions = flattenOrgUnitTree(orgUnits);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="newAssignedToUserId">Nuevo destinatario</Label>
        <Select name="newAssignedToUserId" required>
          <SelectTrigger id="newAssignedToUserId" className="w-full">
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

      {accessories.length > 0 && (
        <div className="flex flex-col gap-2">
          <Label>Accesorios a incluir</Label>
          {accessories.map((accessory) => (
            <label key={accessory.id} className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                name="accessoryAssetIds"
                value={accessory.id}
                defaultChecked={accessory.status === "Assigned"}
                className="size-4"
              />
              {accessory.internalFolio} — {accessory.brand} {accessory.model}
              {accessory.status === "InWarehouse" && (
                <span className="text-muted-foreground text-xs">(nuevo, no estaba en el paquete actual)</span>
              )}
            </label>
          ))}
        </div>
      )}

      <div className="flex flex-col gap-1">
        <Label htmlFor="typedFullName">Tu nombre completo (confirma la devolución del custodio actual)</Label>
        <Input id="typedFullName" name="typedFullName" required maxLength={200} />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="notes">Notas (opcional)</Label>
        <Input id="notes" name="notes" maxLength={500} />
      </div>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Reasignar"}
        </Button>
      </div>
    </form>
  );
}
