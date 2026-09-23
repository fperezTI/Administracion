"use client";

import { useTransition, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import type { AssetCategorySummary } from "@/lib/api";
import { ASSET_STATUS_LABELS } from "@/lib/asset-labels";

/** Navega vía el router de Next (en vez de una recarga completa con <form method="GET">) para que
 * assets/loading.tsx pueda mostrar un estado de carga visible mientras se filtra — un GET nativo
 * solo deja ver el ícono de carga del navegador, que pasa desapercibido (reportado por Francisco). */
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
  const router = useRouter();
  const [isPending, startTransition] = useTransition();

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const query = new URLSearchParams();
    for (const [key, value] of formData.entries()) {
      if (typeof value === "string" && value !== "") {
        query.set(key, value);
      }
    }
    startTransition(() => {
      router.push(`/assets?${query.toString()}`);
    });
  }

  return (
    <form onSubmit={handleSubmit} className="mb-4 flex flex-wrap items-end gap-3">
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
      <Button type="submit" variant="outline" disabled={isPending}>
        {isPending ? <Loader2 data-icon="inline-start" className="animate-spin" /> : null}
        {isPending ? "Filtrando…" : "Filtrar"}
      </Button>
    </form>
  );
}
