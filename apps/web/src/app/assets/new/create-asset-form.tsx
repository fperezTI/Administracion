"use client";

import { useActionState, useState } from "react";
import type { AssetCategoryDetail, CustomFieldDefinitionSummary, OrgUnitNode } from "@/lib/api";
import { flattenOrgUnitTree } from "@/lib/org-unit-tree";
import { PHYSICAL_CONDITION_LABELS } from "@/lib/asset-labels";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Field } from "@/components/ui/field";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createAssetAction, type CreateAssetActionState } from "./actions";

const initialState: CreateAssetActionState = { error: null };

export function CreateAssetForm({
  companyId,
  categories,
  orgUnits,
}: {
  companyId: string;
  categories: AssetCategoryDetail[];
  orgUnits: OrgUnitNode[];
}) {
  const orgUnitOptions = flattenOrgUnitTree(orgUnits);
  const [state, formAction, pending] = useActionState(createAssetAction, initialState);
  const [categoryId, setCategoryId] = useState(categories[0]?.id ?? "");
  const selectedCategory = categories.find((c) => c.id === categoryId);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="companyId" value={companyId} />

      <Field label="Categoría" htmlFor="assetCategoryId">
        <Select name="assetCategoryId" value={categoryId} onValueChange={(value) => setCategoryId(value ?? "")} required>
          <SelectTrigger id="assetCategoryId" className="w-full">
            <SelectValue>{(value: string) => categories.find((c) => c.id === value)?.name}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            {categories.map((category) => (
              <SelectItem key={category.id} value={category.id}>
                {category.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Field label="Marca" htmlFor="brand">
          <Input id="brand" name="brand" required maxLength={100} />
        </Field>
        <Field label="Modelo" htmlFor="model">
          <Input id="model" name="model" required maxLength={100} />
        </Field>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Field label="Número de serie" htmlFor="serialNumber" optional>
          <Input id="serialNumber" name="serialNumber" maxLength={100} />
        </Field>
        <Field label="Condición física" htmlFor="physicalCondition">
          <Select name="physicalCondition" defaultValue="Good" required>
            <SelectTrigger id="physicalCondition" className="w-full">
              <SelectValue>{(value: keyof typeof PHYSICAL_CONDITION_LABELS) => PHYSICAL_CONDITION_LABELS[value]}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              {Object.entries(PHYSICAL_CONDITION_LABELS).map(([value, label]) => (
                <SelectItem key={value} value={value}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
      </div>

      <Field label="Descripción" htmlFor="description" optional>
        <Input id="description" name="description" maxLength={500} />
      </Field>

      <Field label="Ubicación" htmlFor="currentOrgUnitId" optional>
        <Select name="currentOrgUnitId" defaultValue="">
          <SelectTrigger id="currentOrgUnitId" className="w-full">
            <SelectValue>
              {(value: string) => (value === "" ? "Sin asignar" : orgUnitOptions.find((o) => o.id === value)?.label)}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">Sin asignar</SelectItem>
            {orgUnitOptions.map((option) => (
              <SelectItem key={option.id} value={option.id}>
                {option.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {orgUnitOptions.length === 0 && (
          <p className="text-muted-foreground text-xs">
            Esta empresa todavía no tiene estructura organizacional configurada.
          </p>
        )}
      </Field>

      {selectedCategory && selectedCategory.customFields.length > 0 && (
        <fieldset className="flex flex-col gap-4 rounded-lg border p-4">
          <legend className="text-muted-foreground px-1 text-xs font-medium">
            Campos técnicos de {selectedCategory.name}
          </legend>
          {selectedCategory.customFields.map((field) => (
            <CustomFieldInput key={field.id} field={field} />
          ))}
        </fieldset>
      )}

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Guardar y emitir etiqueta"}
        </Button>
      </div>
    </form>
  );
}

function CustomFieldInput({ field }: { field: CustomFieldDefinitionSummary }) {
  const name = `customField:${field.id}`;
  const htmlId = `customField-${field.id}`;

  return (
    <Field label={field.name} htmlFor={htmlId} optional={!field.isRequired}>
      {field.dataType === "Boolean" ? (
        <Select name={name} required={field.isRequired}>
          <SelectTrigger id={htmlId} className="w-full">
            <SelectValue placeholder="Selecciona">{(value: string) => (value === "true" ? "Sí" : "No")}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="true">Sí</SelectItem>
            <SelectItem value="false">No</SelectItem>
          </SelectContent>
        </Select>
      ) : field.dataType === "Select" ? (
        <Select name={name} required={field.isRequired}>
          <SelectTrigger id={htmlId} className="w-full">
            <SelectValue placeholder="Selecciona" />
          </SelectTrigger>
          <SelectContent>
            {(field.options ?? "")
              .split(",")
              .map((option) => option.trim())
              .filter(Boolean)
              .map((option) => (
                <SelectItem key={option} value={option}>
                  {option}
                </SelectItem>
              ))}
          </SelectContent>
        </Select>
      ) : (
        <Input
          id={htmlId}
          name={name}
          required={field.isRequired}
          type={field.dataType === "Number" ? "number" : field.dataType === "Date" ? "date" : "text"}
          step={field.dataType === "Number" ? "any" : undefined}
        />
      )}
    </Field>
  );
}
