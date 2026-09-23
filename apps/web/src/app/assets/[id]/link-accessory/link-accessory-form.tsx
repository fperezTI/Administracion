"use client";

import { useActionState } from "react";
import type { AccessoryCandidate } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Combobox,
  ComboboxContent,
  ComboboxEmpty,
  ComboboxInput,
  ComboboxItem,
  ComboboxList,
} from "@/components/ui/combobox";
import { linkAccessoryAction, type LinkAccessoryActionState } from "./actions";

const initialState: LinkAccessoryActionState = { error: null };

function candidateLabel(candidate: AccessoryCandidate) {
  return `${candidate.internalFolio} — ${candidate.brand} ${candidate.model}`;
}

function matchesQuery(candidate: AccessoryCandidate, query: string) {
  const normalizedQuery = query.trim().toLowerCase();
  if (!normalizedQuery) {
    return true;
  }
  return [candidate.internalFolio, candidate.brand, candidate.model, candidate.serialNumber ?? ""].some((field) =>
    field.toLowerCase().includes(normalizedQuery),
  );
}

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
        <Combobox
          items={candidates}
          itemToStringLabel={candidateLabel}
          itemToStringValue={(candidate: AccessoryCandidate) => candidate.id}
          isItemEqualToValue={(a: AccessoryCandidate, b: AccessoryCandidate) => a.id === b.id}
          filter={matchesQuery}
          name="accessoryAssetId"
          required
          disabled={candidates.length === 0}
        >
          <ComboboxInput
            id="accessoryAssetId"
            showClear
            placeholder="Busca por folio, marca, modelo o número de serie…"
          />
          <ComboboxContent>
            <ComboboxEmpty>No se encontraron activos.</ComboboxEmpty>
            <ComboboxList>
              {(candidate: AccessoryCandidate) => (
                <ComboboxItem key={candidate.id} value={candidate}>
                  <div className="flex flex-col">
                    <span>{candidateLabel(candidate)}</span>
                    <span className="text-muted-foreground text-xs">
                      Serie: {candidate.serialNumber ?? "—"}
                    </span>
                  </div>
                </ComboboxItem>
              )}
            </ComboboxList>
          </ComboboxContent>
        </Combobox>
        {candidates.length === 0 && (
          <p className="text-muted-foreground text-xs">No hay activos disponibles para vincular como accesorio.</p>
        )}
      </div>

      {state.error && (
        <p className="text-destructive text-sm" role="alert">
          {state.error}
        </p>
      )}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending || candidates.length === 0}>
          {pending ? "Vinculando…" : "Vincular"}
        </Button>
      </div>
    </form>
  );
}
