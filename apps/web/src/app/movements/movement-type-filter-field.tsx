"use client";

import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { MOVEMENT_TYPE_LABELS } from "@/lib/inventory-labels";

export function MovementTypeFilterField({ defaultType }: { defaultType: string }) {
  return (
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
  );
}
