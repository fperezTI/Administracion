"use client";

import { useState, useActionState } from "react";
import { getImportTemplateDownloadUrl, type AssetCategorySummary } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { uploadImportBatchAction, type UploadImportBatchActionState } from "./actions";

const initialState: UploadImportBatchActionState = { error: null };

export function UploadImportBatchForm({ companyId, categories }: { companyId: string; categories: AssetCategorySummary[] }) {
  const boundAction = uploadImportBatchAction.bind(null, companyId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);
  const [templateCategoryId, setTemplateCategoryId] = useState(categories[0]?.id ?? "");

  return (
    <div className="flex flex-col gap-6">
      <div className="rounded-lg border p-4">
        <Label htmlFor="templateCategoryId">Plantilla CSV por categoría</Label>
        <p className="text-muted-foreground mb-2 text-xs">
          Descarga la plantilla con las columnas exactas (incluye los campos personalizados de la categoría elegida).
        </p>
        <div className="flex items-end gap-2">
          <Select value={templateCategoryId} onValueChange={(value) => setTemplateCategoryId(value ?? "")}>
            <SelectTrigger id="templateCategoryId" className="w-full flex-1">
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
          <Button
            type="button"
            variant="outline"
            size="sm"
            render={<a href={getImportTemplateDownloadUrl(templateCategoryId)} />}
            disabled={!templateCategoryId}
          >
            Descargar plantilla
          </Button>
        </div>
      </div>

      <form action={formAction} className="flex flex-col gap-3 rounded-lg border p-4">
        <Label htmlFor="file">Archivo CSV</Label>
        <input
          type="file"
          id="file"
          name="file"
          accept=".csv,text/csv"
          required
          className="border-input bg-transparent file:bg-secondary file:text-secondary-foreground rounded-lg border text-sm outline-none file:mr-3 file:h-8 file:cursor-pointer file:rounded-l-lg file:border-0 file:px-3"
        />
        {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}
        <Button type="submit" disabled={pending}>
          {pending ? "Subiendo…" : "Subir e iniciar validación"}
        </Button>
      </form>
    </div>
  );
}
