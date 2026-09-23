"use client";

import { useState } from "react";
import { Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import type { AssetCategorySummary } from "@/lib/api";
import { ASSET_STATUS_LABELS } from "@/lib/asset-labels";

/** Sigue siendo un <form method="GET"> nativo (recarga completa) — es lo que ya funcionaba y es
 * robusto. Lo único que agrega JS es un overlay de carga que se dispara en onSubmit SIN
 * preventDefault: React alcanza a pintarlo antes de que el navegador navegue, y desaparece solo
 * cuando la página nueva reemplaza a esta. (Un intento anterior interceptaba el submit con
 * router.push()+useTransition para poder mostrar assets/loading.tsx, pero eso suprime el fallback
 * de Suspense en esa transición — con una consulta lenta se veía "colgado" sin filtrar. Este es más
 * simple y no depende de ese comportamiento de Next.) */
export function AssetFilterForm({
  companyId,
  categories,
  defaultCategoryId,
  defaultStatus,
  defaultSearch,
}: {
  companyId: string;
  categories: AssetCategorySummary[];
  defaultCategoryId: string;
  defaultStatus: string;
  defaultSearch: string;
}) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  return (
    <>
      <form method="GET" onSubmit={() => setIsSubmitting(true)} className="mb-4 flex flex-wrap items-end gap-3">
        <input type="hidden" name="companyId" value={companyId} />
        <div className="flex flex-col gap-1">
          <Label htmlFor="assetCategoryId">Categoría</Label>
          <Select name="assetCategoryId" defaultValue={defaultCategoryId}>
            <SelectTrigger id="assetCategoryId" className="w-full">
              <SelectValue>
                {(value: string) => (value === "" ? "Todas" : categories.find((c) => c.id === value)?.name)}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">Todas</SelectItem>
              {categories.map((category) => (
                <SelectItem key={category.id} value={category.id}>
                  {category.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="status">Estado</Label>
          <Select name="status" defaultValue={defaultStatus}>
            <SelectTrigger id="status" className="w-full">
              <SelectValue>
                {(value: string) => (value === "" ? "Todos" : ASSET_STATUS_LABELS[value as keyof typeof ASSET_STATUS_LABELS])}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">Todos</SelectItem>
              {Object.entries(ASSET_STATUS_LABELS).map(([value, label]) => (
                <SelectItem key={value} value={value}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="search">Buscar</Label>
          <Input id="search" name="search" defaultValue={defaultSearch} placeholder="Folio, marca, modelo, serie" />
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
          <p className="text-foreground text-sm font-medium">Buscando activos…</p>
        </div>
      )}
    </>
  );
}
