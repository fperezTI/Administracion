"use client";

import { useActionState, useMemo, useState } from "react";
import type { AssetSummary, InternalRequestType } from "@/lib/api";
import { INTERNAL_REQUEST_TYPE_LABELS } from "@/lib/request-labels";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createRequestAction, type CreateRequestActionState } from "./actions";

const initialState: CreateRequestActionState = { error: null };

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
        <Select
          name="type"
          required
          value={type}
          onValueChange={(value) => setType((value ?? "") as InternalRequestType)}
        >
          <SelectTrigger id="type" className="w-full">
            <SelectValue placeholder="Selecciona">
              {(value: keyof typeof INTERNAL_REQUEST_TYPE_LABELS) => INTERNAL_REQUEST_TYPE_LABELS[value]}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {Object.entries(INTERNAL_REQUEST_TYPE_LABELS).map(([value, label]) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="assetId">
          {type === "Maintenance" ? "Activo (en almacén o asignado a ti)" : "Activo (debe estar en almacén)"}
        </Label>
        <Select name="assetId" required>
          <SelectTrigger id="assetId" className="w-full">
            <SelectValue placeholder="Selecciona">
              {(value: string) => {
                const asset = eligibleAssets.find((a) => a.id === value);
                return asset ? `${asset.internalFolio} — ${asset.brand} ${asset.model}` : null;
              }}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {eligibleAssets.map((asset) => (
              <SelectItem key={asset.id} value={asset.id}>
                {asset.internalFolio} — {asset.brand} {asset.model}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
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

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Enviando…" : "Enviar solicitud"}
        </Button>
      </div>
    </form>
  );
}
