"use client";

import { useActionState, useMemo, useState } from "react";
import type { AssetSummary, InternalRequestType } from "@/lib/api";
import { INTERNAL_REQUEST_TYPE_LABELS } from "@/lib/request-labels";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createRequestAction, type CreateRequestActionState } from "./actions";

const initialState: CreateRequestActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

export function CreateRequestForm({
  inWarehouseAssets,
  assignedAssets,
  companyId,
}: {
  inWarehouseAssets: AssetSummary[];
  assignedAssets: AssetSummary[];
  companyId: string;
}) {
  const [state, formAction, pending] = useActionState(createRequestAction, initialState);
  const [type, setType] = useState<InternalRequestType | "">("");

  const eligibleAssets = useMemo(() => {
    if (type === "Maintenance") {
      return [...inWarehouseAssets, ...assignedAssets];
    }
    return inWarehouseAssets;
  }, [type, inWarehouseAssets, assignedAssets]);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="companyId" value={companyId} />

      <div className="flex flex-col gap-1">
        <Label htmlFor="type">Tipo de solicitud</Label>
        <select
          id="type"
          name="type"
          className={selectClassName}
          required
          value={type}
          onChange={(e) => setType(e.target.value as InternalRequestType)}
        >
          <option value="" disabled>
            Selecciona
          </option>
          {Object.entries(INTERNAL_REQUEST_TYPE_LABELS).map(([value, label]) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="assetId">
          {type === "Maintenance" ? "Activo (en almacén o asignado a ti)" : "Activo (debe estar en almacén)"}
        </Label>
        <select id="assetId" name="assetId" className={selectClassName} required defaultValue="">
          <option value="" disabled>
            Selecciona
          </option>
          {eligibleAssets.map((asset) => (
            <option key={asset.id} value={asset.id}>
              {asset.internalFolio} — {asset.brand} {asset.model}
            </option>
          ))}
        </select>
        {type && eligibleAssets.length === 0 && (
          <p className="text-muted-foreground text-xs">No hay activos elegibles para este tipo de solicitud.</p>
        )}
      </div>

      {type === "Loan" && (
        <div className="flex flex-col gap-1">
          <Label htmlFor="expectedReturnDate">Fecha esperada de devolución</Label>
          <Input id="expectedReturnDate" name="expectedReturnDate" type="date" required />
        </div>
      )}

      <div className="flex flex-col gap-1">
        <Label htmlFor="justification">Justificación</Label>
        <textarea
          id="justification"
          name="justification"
          required
          maxLength={1000}
          rows={4}
          placeholder="Explica por qué haces esta solicitud"
          className="rounded-lg border border-input bg-transparent px-2.5 py-2 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30"
        />
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Enviando…" : "Enviar solicitud"}
        </Button>
      </div>
    </form>
  );
}
