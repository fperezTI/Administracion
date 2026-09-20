import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssetCategoryById } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { CUSTOM_FIELD_DATA_TYPE_LABELS, IDENTIFICATION_TECHNOLOGY_LABELS } from "@/lib/asset-labels";
import { AddFieldForm } from "./add-field-form";
import { toggleCategoryActiveAction } from "./actions";

export default async function AssetCategoryDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let category;
  try {
    category = await getAssetCategoryById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  return (
    <>
      <AppHeader title="Categorías de activos" />
    <div className="mx-auto flex max-w-3xl flex-col gap-4 px-8 pb-8">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold tracking-tight">{category.name}</h2>
          <p className="text-muted-foreground font-mono text-xs">{category.code}</p>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={category.isActive ? "success" : "outline"}>
            {category.isActive ? "Activa" : "Inactiva"}
          </Badge>
          <form action={toggleCategoryActiveAction.bind(null, id, !category.isActive)}>
            <Button type="submit" variant="outline" size="sm">
              {category.isActive ? "Desactivar" : "Activar"}
            </Button>
          </form>
        </div>
      </div>

      <p className="text-sm">
        Tecnología de identificación por defecto:{" "}
        <strong>{IDENTIFICATION_TECHNOLOGY_LABELS[category.defaultIdentificationTechnology]}</strong>
      </p>

        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Nombre</TableHead>
              <TableHead>Código</TableHead>
              <TableHead>Tipo</TableHead>
              <TableHead>Obligatorio</TableHead>
              <TableHead>Opciones</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {category.customFields.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-muted-foreground py-6 text-center">
                  Esta categoría todavía no tiene campos técnicos.
                </TableCell>
              </TableRow>
            ) : (
              category.customFields.map((field) => (
                <TableRow key={field.id}>
                  <TableCell className="font-medium">{field.name}</TableCell>
                  <TableCell className="font-mono text-xs">{field.code}</TableCell>
                  <TableCell>{CUSTOM_FIELD_DATA_TYPE_LABELS[field.dataType]}</TableCell>
                  <TableCell>{field.isRequired ? "Sí" : "No"}</TableCell>
                  <TableCell>{field.options ?? "—"}</TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>

      <AddFieldForm categoryId={id} />

      <Button variant="outline" render={<Link href="/asset-categories" />} className="self-start">
        ← Volver
      </Button>
    </div>
    </>
  );
}
