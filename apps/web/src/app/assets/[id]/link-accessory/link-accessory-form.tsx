"use client";

import { useActionState } from "react";
import type { AccessoryCandidate } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { linkAccessoryAction, type LinkAccessoryActionState } from "./actions";

const initialState: LinkAccessoryActionState = { error: null };

export function LinkAccessoryForm({
  primaryAssetId,
  candidates,
}: {
  primaryAssetId: string;
  candidates: AccessoryCandidate[];
}) {
  const boundAction = linkAccessoryAction.bind(null, primaryAssetId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="accessoryAssetId">Activo a vincular</Label>
        <Select name="accessoryAssetId" required>
          <SelectTrigger id="accessoryAssetId" className="w-full">
            <SelectValue placeholder="Selecciona">
              {(value: string) => {
                const candidate = candidates.find((c) => c.id === value);
                return candidate ? `${candidate.internalFolio} — ${candidate.brand} ${candidate.model}` : null;
              }}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {candidates.map((candidate) => (
              <SelectItem key={candidate.id} value={candidate.id}>
                {candidate.internalFolio} — {candidate.brand} {candidate.model}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {candidates.length === 0 && (
          <p className="text-muted-foreground text-xs">No hay activos disponibles para vincular como accesorio.</p>
        )}
      </div>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending || candidates.length === 0}>
          {pending ? "Vinculando…" : "Vincular"}
        </Button>
      </div>
    </form>
  );
}
