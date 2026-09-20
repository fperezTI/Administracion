"use client";

import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

export function MoveOrgUnitSelect({
  name,
  defaultValue,
  options,
}: {
  name: string;
  defaultValue: string;
  options: { id: string; label: string }[];
}) {
  return (
    <Select name={name} defaultValue={defaultValue}>
      <SelectTrigger size="sm" className="w-full">
        <SelectValue>
          {(value: string) => (value === "" ? "Ninguna (raíz)" : options.find((o) => o.id === value)?.label)}
        </SelectValue>
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="">Ninguna (raíz)</SelectItem>
        {options.map((o) => (
          <SelectItem key={o.id} value={o.id}>
            {o.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
