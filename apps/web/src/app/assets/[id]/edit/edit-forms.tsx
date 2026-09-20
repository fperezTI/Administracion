"use client";

import Link from "next/link";
import { useActionState } from "react";
import type { AssetCategoryDetail, AssetDetail, CustomFieldDefinitionSummary } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  updateContractualAction,
  updateFinancialAction,
  updateGeneralAction,
  type EditActionState,
} from "./actions";

const editInitialState: EditActionState = { error: null, success: false };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

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

function SaveBar({ error, success, pending }: { error: string | null; success: boolean; pending: boolean }) {
  return (
    <div className="flex items-center justify-between">
      <p className="text-sm">
        {error && <span className="text-destructive">{error}</span>}
        {!error && success && <span className="text-green-600 dark:text-green-500">Guardado.</span>}
      </p>
      <Button type="submit" disabled={pending} size="sm">
        {pending ? "Guardando…" : "Guardar"}
      </Button>
    </div>
  );
}

export function GeneralInfoForm({
  asset,
  category,
}: {
  asset: AssetDetail;
  category: AssetCategoryDetail | null;
}) {
  const [state, formAction, pending] = useActionState(updateGeneralAction, editInitialState);
  const valueByFieldId = new Map(asset.customFieldValues.map((v) => [v.customFieldDefinitionId, v.value]));

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm">General</CardTitle>
      </CardHeader>
      <CardContent>
        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="assetId" value={asset.id} />

          <div className="grid grid-cols-2 gap-4">
            <Field label="Marca" htmlFor="brand">
              <Input id="brand" name="brand" defaultValue={asset.brand} required maxLength={100} />
            </Field>
            <Field label="Modelo" htmlFor="model">
              <Input id="model" name="model" defaultValue={asset.model} required maxLength={100} />
            </Field>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <Field label="Número de serie" htmlFor="serialNumber" optional>
              <Input id="serialNumber" name="serialNumber" defaultValue={asset.serialNumber ?? ""} maxLength={100} />
            </Field>
            <Field label="Folio patrimonial" htmlFor="patrimonialFolio" optional>
              <Input
                id="patrimonialFolio"
                name="patrimonialFolio"
                defaultValue={asset.patrimonialFolio ?? ""}
                maxLength={100}
              />
            </Field>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <Field label="Condición física" htmlFor="physicalCondition">
              <select
                id="physicalCondition"
                name="physicalCondition"
                defaultValue={asset.physicalCondition}
                className={selectClassName}
                required
              >
                <option value="Excellent">Excelente</option>
                <option value="Good">Buena</option>
                <option value="Fair">Regular</option>
                <option value="Poor">Mala</option>
                <option value="Damaged">Dañado</option>
              </select>
            </Field>
            <div className="flex flex-col gap-1">
              <Label>Ubicación</Label>
              <Link href={`/assets/${asset.id}/relocate`} className="text-primary text-sm hover:underline">
                Reubicar activo →
              </Link>
              <p className="text-muted-foreground text-xs">
                La ubicación se cambia desde un movimiento auditado, no desde este formulario.
              </p>
            </div>
          </div>

          <Field label="Descripción" htmlFor="description" optional>
            <Input id="description" name="description" defaultValue={asset.description ?? ""} maxLength={500} />
          </Field>

          {category && category.customFields.length > 0 && (
            <fieldset className="flex flex-col gap-4 rounded-lg border p-4">
              <legend className="text-muted-foreground px-1 text-xs font-medium">
                Campos técnicos de {category.name}
              </legend>
              {category.customFields.map((field) => (
                <CustomFieldInput key={field.id} field={field} defaultValue={valueByFieldId.get(field.id) ?? ""} />
              ))}
            </fieldset>
          )}

          <SaveBar error={state.error} success={state.success} pending={pending} />
        </form>
      </CardContent>
    </Card>
  );
}

function CustomFieldInput({ field, defaultValue }: { field: CustomFieldDefinitionSummary; defaultValue: string }) {
  const name = `customField:${field.id}`;
  const htmlId = `customField-${field.id}`;

  return (
    <Field label={field.name} htmlFor={htmlId} optional={!field.isRequired}>
      {field.dataType === "Boolean" ? (
        <select id={htmlId} name={name} className={selectClassName} required={field.isRequired} defaultValue={defaultValue}>
          <option value="" disabled>
            Selecciona
          </option>
          <option value="true">Sí</option>
          <option value="false">No</option>
        </select>
      ) : field.dataType === "Select" ? (
        <select id={htmlId} name={name} className={selectClassName} required={field.isRequired} defaultValue={defaultValue}>
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
          defaultValue={defaultValue}
          required={field.isRequired}
          type={field.dataType === "Number" ? "number" : field.dataType === "Date" ? "date" : "text"}
          step={field.dataType === "Number" ? "any" : undefined}
        />
      )}
    </Field>
  );
}

export function FinancialInfoForm({ asset }: { asset: AssetDetail }) {
  const [state, formAction, pending] = useActionState(updateFinancialAction, editInitialState);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm">Información financiera (informativa)</CardTitle>
      </CardHeader>
      <CardContent>
        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="assetId" value={asset.id} />

          <div className="grid grid-cols-2 gap-4">
            <Field label="Fecha de adquisición" htmlFor="acquisitionDate" optional>
              <Input id="acquisitionDate" name="acquisitionDate" type="date" defaultValue={asset.acquisitionDate ?? ""} />
            </Field>
            <Field label="Costo" htmlFor="acquisitionCost" optional>
              <Input
                id="acquisitionCost"
                name="acquisitionCost"
                type="number"
                step="0.01"
                min="0"
                defaultValue={asset.acquisitionCost ?? ""}
              />
            </Field>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <Field label="Moneda (ISO 4217)" htmlFor="currency" optional>
              <Input id="currency" name="currency" maxLength={3} defaultValue={asset.currency ?? ""} />
            </Field>
            <Field label="Proveedor" htmlFor="supplier" optional>
              <Input id="supplier" name="supplier" maxLength={200} defaultValue={asset.supplier ?? ""} />
            </Field>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <Field label="Factura" htmlFor="invoice" optional>
              <Input id="invoice" name="invoice" maxLength={100} defaultValue={asset.invoice ?? ""} />
            </Field>
            <Field label="Orden de compra" htmlFor="purchaseOrder" optional>
              <Input id="purchaseOrder" name="purchaseOrder" maxLength={100} defaultValue={asset.purchaseOrder ?? ""} />
            </Field>
          </div>

          <SaveBar error={state.error} success={state.success} pending={pending} />
        </form>
      </CardContent>
    </Card>
  );
}

export function ContractualInfoForm({ asset }: { asset: AssetDetail }) {
  const [state, formAction, pending] = useActionState(updateContractualAction, editInitialState);

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm">Garantía y soporte</CardTitle>
      </CardHeader>
      <CardContent>
        <form action={formAction} className="flex flex-col gap-4">
          <input type="hidden" name="assetId" value={asset.id} />

          <div className="grid grid-cols-2 gap-4">
            <Field label="Inicio de garantía" htmlFor="warrantyStartDate" optional>
              <Input
                id="warrantyStartDate"
                name="warrantyStartDate"
                type="date"
                defaultValue={asset.warrantyStartDate ?? ""}
              />
            </Field>
            <Field label="Fin de garantía" htmlFor="warrantyEndDate" optional>
              <Input id="warrantyEndDate" name="warrantyEndDate" type="date" defaultValue={asset.warrantyEndDate ?? ""} />
            </Field>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <Field label="Contrato de soporte" htmlFor="supportContract" optional>
              <Input id="supportContract" name="supportContract" maxLength={100} defaultValue={asset.supportContract ?? ""} />
            </Field>
            <Field label="Proveedor de soporte" htmlFor="supportProvider" optional>
              <Input
                id="supportProvider"
                name="supportProvider"
                maxLength={200}
                defaultValue={asset.supportProvider ?? ""}
              />
            </Field>
          </div>

          <SaveBar error={state.error} success={state.success} pending={pending} />
        </form>
      </CardContent>
    </Card>
  );
}
