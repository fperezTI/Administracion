"use client";

import { useState } from "react";
import { Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

/** Mismo patrón que apps/web/src/app/assets/asset-filter-form.tsx: <form method="GET"> nativo (el
 * filtro de auditoría ya lo tenía) + overlay de carga disparado en onSubmit sin preventDefault. De
 * paso reemplaza el <button> con clases sueltas que tenía este formulario por el componente Button
 * del sistema de diseño (era la única pantalla que no lo usaba). */
export function AuditFilterForm({
  defaultCommandName,
  defaultFromUtc,
  defaultToUtc,
}: {
  defaultCommandName: string | undefined;
  defaultFromUtc: string | undefined;
  defaultToUtc: string | undefined;
}) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  return (
    <>
      <form method="GET" onSubmit={() => setIsSubmitting(true)} className="mb-4 flex flex-wrap items-end gap-3">
        <div className="flex flex-col gap-1">
          <Label htmlFor="commandName">Comando</Label>
          <Input
            id="commandName"
            name="commandName"
            defaultValue={defaultCommandName}
            placeholder="CreateAssetCommand"
            className="h-8 w-56"
          />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="fromUtc">Desde</Label>
          <Input id="fromUtc" name="fromUtc" type="date" defaultValue={defaultFromUtc} className="h-8" />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="toUtc">Hasta</Label>
          <Input id="toUtc" name="toUtc" type="date" defaultValue={defaultToUtc} className="h-8" />
        </div>
        <Button type="submit" variant="outline" size="sm" disabled={isSubmitting}>
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
          <p className="text-foreground text-sm font-medium">Buscando en la auditoría…</p>
        </div>
      )}
    </>
  );
}
