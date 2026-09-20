"use client";

import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

export function AssignSelect({
  name,
  placeholder,
  options,
}: {
  name: string;
  placeholder: string;
  options: { id: string; label: string }[];
}) {
  return (
    <Select name={name} required>
      <SelectTrigger className="w-full">
        <SelectValue placeholder={placeholder}>{(value: string) => options.find((o) => o.id === value)?.label}</SelectValue>
      </SelectTrigger>
      <SelectContent>
        {options.map((o) => (
          <SelectItem key={o.id} value={o.id}>
            {o.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
