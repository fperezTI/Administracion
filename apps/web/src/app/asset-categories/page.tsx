import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssetCategories } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { IDENTIFICATION_TECHNOLOGY_LABELS } from "@/lib/asset-labels";

export default async function AssetCategoriesPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const categories = await getAssetCategories(accessToken, { pageSize: 200 });
    content = (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Nombre</TableHead>
              <TableHead>Código</TableHead>
              <TableHead>Tecnología por defecto</TableHead>
              <TableHead>Campos personalizados</TableHead>
              <TableHead>Estado</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {categories.items.map((category) => (
              <TableRow key={category.id}>
                <TableCell className="font-medium">
                  <Link href={`/asset-categories/${category.id}`} className="hover:underline">
                    {category.name}
                  </Link>
                </TableCell>
                <TableCell className="font-mono text-xs">{category.code}</TableCell>
                <TableCell>{IDENTIFICATION_TECHNOLOGY_LABELS[category.defaultIdentificationTechnology]}</TableCell>
                <TableCell>{category.customFieldCount}</TableCell>
                <TableCell>
                  <Badge variant={category.isActive ? "success" : "outline"}>
                    {category.isActive ? "Activa" : "Inactiva"}
                  </Badge>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403
          ? "No tienes permiso para consultar categorías (Catalogs.Read)."
          : "No fue posible consultar las categorías."}
      </p>
    );
  }

  return (
    <>
      <AppHeader title="Categorías de activos" subtitle="Catálogo configurable — define los campos técnicos por categoría." />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href="/asset-categories/new" />}>Nueva categoría</Button>
      </div>
      {content}
    </div>
    </>
  );
}
