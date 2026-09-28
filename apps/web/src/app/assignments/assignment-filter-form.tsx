"use client";

import { useState } from "react";
import { Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { ASSIGNMENT_STATUS_LABELS } from "@/lib/inventory-labels";
import type { OrgUnitOption } from "@/lib/org-unit-tree";

/** Mismo patrón que AssetFilterForm (apps/web/src/app/assets/asset-filter-form.tsx): <form method="GET">
 * nativo con overlay de carga, sin interceptar el submit — ver el comentario de ese archivo para el
 * razonamiento completo. */
export function AssignmentFilterForm({
  companyId,
  orgUnits,
  defaultStatus,
  defaultAssignedToSearch,
  defaultSearch,
  defaultAssignedFrom,
  defaultAssignedTo,
  defaultOrgUnitId,
}: {
  companyId: string;
  orgUnits: OrgUnitOption[];
  defaultStatus: string;
  defaultAssignedToSearch: string;
  defaultSearch: string;
  defaultAssignedFrom: string;
  defaultAssignedTo: string;
  defaultOrgUnitId: string;
}) {
  const [isSubmitting, setIsSubmitting] = useState(false);

  return (
    <>
      <form method="GET" onSubmit={() => setIsSubmitting(true)} className="mb-4 flex flex-wrap items-end gap-3">
        <input type="hidden" name="companyId" value={companyId} />
        <div className="flex flex-col gap-1">
          <Label htmlFor="status">Estado</Label>
          <Select name="status" defaultValue={defaultStatus}>
            <SelectTrigger id="status" className="w-full">
              <SelectValue>
                {(value: string) =>
                  value === "" ? "Todos" : ASSIGNMENT_STATUS_LABELS[value as keyof typeof ASSIGNMENT_STATUS_LABELS]
                }
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">Todos</SelectItem>
              {Object.entries(ASSIGNMENT_STATUS_LABELS).map(([value, label]) => (
                <SelectItem key={value} value={value}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="assignedToSearch">Persona asignada</Label>
          <Input
            id="assignedToSearch"
            name="assignedToSearch"
            defaultValue={defaultAssignedToSearch}
            placeholder="Nombre"
          />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="search">Activo</Label>
          <Input id="search" name="search" defaultValue={defaultSearch} placeholder="Folio, marca, modelo" />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="orgUnitId">Área</Label>
          <Select name="orgUnitId" defaultValue={defaultOrgUnitId}>
            <SelectTrigger id="orgUnitId" className="w-full">
              <SelectValue>
                {(value: string) => (value === "" ? "Todas" : orgUnits.find((o) => o.id === value)?.label)}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">Todas</SelectItem>
              {orgUnits.map((option) => (
                <SelectItem key={option.id} value={option.id}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="assignedFrom">Asignado desde</Label>
          <Input id="assignedFrom" name="assignedFrom" type="date" defaultValue={defaultAssignedFrom} />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="assignedTo">Asignado hasta</Label>
          <Input id="assignedTo" name="assignedTo" type="date" defaultValue={defaultAssignedTo} />
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
          <p className="text-foreground text-sm font-medium">Buscando asignaciones…</p>
        </div>
      )}
    </>
  );
}
