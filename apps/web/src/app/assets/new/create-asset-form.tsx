"use client";

import { useActionState, useState } from "react";
import type { AssetCategoryDetail, CustomFieldDefinitionSummary, OrgUnitNode } from "@/lib/api";
import { flattenOrgUnitTree } from "@/lib/org-unit-tree";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createAssetAction, type CreateAssetActionState } from "./actions";

const initialState: CreateAssetActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

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
        <select
          id="assetCategoryId"
          name="assetCategoryId"
          value={categoryId}
          onChange={(e) => setCategoryId(e.target.value)}
          className={selectClassName}
          required
        >
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
      </Field>

      <div className="grid grid-cols-2 gap-4">
        <Field label="Marca" htmlFor="brand">
          <Input id="brand" name="brand" required maxLength={100} />
        </Field>
        <Field label="Modelo" htmlFor="model">
          <Input id="model" name="model" required maxLength={100} />
        </Field>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <Field label="Número de serie" htmlFor="serialNumber" optional>
          <Input id="serialNumber" name="serialNumber" maxLength={100} />
        </Field>
        <Field label="Condición física" htmlFor="physicalCondition">
          <select id="physicalCondition" name="physicalCondition" defaultValue="Good" className={selectClassName} required>
            <option value="Excellent">Excelente</option>
            <option value="Good">Buena</option>
            <option value="Fair">Regular</option>
            <option value="Poor">Mala</option>
            <option value="Damaged">Dañado</option>
          </select>
        </Field>
      </div>

      <Field label="Descripción" htmlFor="description" optional>
        <Input id="description" name="description" maxLength={500} />
      </Field>

      <Field label="Ubicación" htmlFor="currentOrgUnitId" optional>
        <select id="currentOrgUnitId" name="currentOrgUnitId" defaultValue="" className={selectClassName}>
          <option value="">Sin asignar</option>
          {orgUnitOptions.map((option) => (
            <option key={option.id} value={option.id}>
              {option.label}
            </option>
          ))}
        </select>
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

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

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
        <select id={htmlId} name={name} className={selectClassName} required={field.isRequired} defaultValue="">
          <option value="" disabled>
            Selecciona
          </option>
          <option value="true">Sí</option>
          <option value="false">No</option>
        </select>
      ) : field.dataType === "Select" ? (
        <select id={htmlId} name={name} className={selectClassName} required={field.isRequired} defaultValue="">
          <option value="" disabled>
            Selecciona
          </option>
          {(field.options ?? "")
            .split(",")
            .map((option) => option.trim())
            .filter(Boolean)
            .map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
        </select>
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

function Field({
  label,
  htmlFor,
  optional,
  children,
}: {
  label: string;
  htmlFor: string;
  optional?: boolean;
  children: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1">
      <Label htmlFor={htmlFor}>
        {label} {optional && <span className="text-muted-foreground font-normal">(opcional)</span>}
      </Label>
      {children}
    </div>
  );
}
