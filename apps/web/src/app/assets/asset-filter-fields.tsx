"use client";

import type { AssetCategorySummary } from "@/lib/api";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { ASSET_STATUS_LABELS } from "@/lib/asset-labels";

export function AssetFilterFields({
  categories,
  defaultCategoryId,
  defaultStatus,
}: {
  categories: AssetCategorySummary[];
  defaultCategoryId: string;
  defaultStatus: string;
}) {
  return (
    <>
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
    </>
  );
}
