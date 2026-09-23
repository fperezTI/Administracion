"use client";

import { useState } from "react";
import { Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { MOVEMENT_TYPE_LABELS } from "@/lib/inventory-labels";

/** Antes este filtro no tenía ningún botón de envío — con un solo Select y sin submit, no había
 * forma real de aplicarlo. Se agrega el botón "Filtrar" junto con el overlay de carga (mismo patrón
 * que apps/web/src/app/assets/asset-filter-form.tsx: <form method="GET"> nativo + onSubmit sin
 * preventDefault para pintar el overlay antes de que el navegador navegue). */
export function MovementFilterForm({ companyId, defaultType }: { companyId: string; defaultType: string }) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  return (
    <>
      <form method="GET" onSubmit={() => setIsSubmitting(true)} className="mb-4 flex flex-wrap items-end gap-3">
        <input type="hidden" name="companyId" value={companyId} />
        <div className="flex flex-col gap-1">
          <Label htmlFor="type">Tipo</Label>
          <Select name="type" defaultValue={defaultType}>
            <SelectTrigger id="type" className="w-full">
              <SelectValue>
                {(value: string) => (value === "" ? "Todos" : MOVEMENT_TYPE_LABELS[value as keyof typeof MOVEMENT_TYPE_LABELS])}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">Todos</SelectItem>
              {Object.entries(MOVEMENT_TYPE_LABELS).map(([value, label]) => (
                <SelectItem key={value} value={value}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <Button type="submit" variant="outline" disabled={isSubmitting}>
          {isSubmitting ? <Loader2 data-icon="inline-start" className="animate-spin" /> : null}
          {isSubmitting ? "Filtrando…" : "Filtrar"}
        </Button>
      </form>

      {isSubmitting && (
        <div
          role="status"
          aria-live="polite"
          className="bg-background/70 fixed inset-0 z-50 flex flex-col items-center justify-center gap-3 backdrop-blur-sm"
        >
          <Loader2 className="text-primary size-8 animate-spin stroke-[1.5]" />
          <p className="text-foreground text-sm font-medium">Buscando movimientos…</p>
        </div>
      )}
    </>
  );
}
