import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssetCategories, type AssetCategorySortField } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHeader, TableRow } from "@/components/ui/table";
import { IDENTIFICATION_TECHNOLOGY_LABELS } from "@/lib/asset-labels";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 50;
const DEFAULT_SORT: AssetCategorySortField = "name";

export default async function AssetCategoriesPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const pageNumber = params.pageNumber ? Math.max(1, Number(params.pageNumber)) : 1;
  const sortBy = (params.sortBy as AssetCategorySortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortDescending === "true";

  let content: React.ReactNode;
  try {
    const categories = await getAssetCategories(accessToken, {
      pageNumber,
      pageSize: PAGE_SIZE,
      sortBy,
      sortDescending,
    });
    const totalPages = Math.max(1, Math.ceil(categories.totalCount / PAGE_SIZE));
    content = (
      <>
        <Table>
          <TableHeader>
            <TableRow>
              {[
                { key: "name", label: "Nombre" },
                { key: "code", label: "Código" },
                { key: "defaultIdentificationTechnology", label: "Tecnología por defecto" },
                { key: "customFieldCount", label: "Campos personalizados" },
                { key: "isActive", label: "Estado" },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/asset-categories"
                  params={params}
                  sortKey={column.key}
                  defaultSortKey={DEFAULT_SORT}
                  currentSortBy={params.sortBy}
                  currentSortDescending={sortDescending}
                >
                  {column.label}
                </SortableTableHead>
              ))}
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
        <TablePagination
          basePath="/asset-categories"
          params={params}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={categories.totalCount}
          itemLabel="categoría"
          itemLabelPlural="categorías"
        />
      </>
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
        <Button render={<Link href="/asset-categories/new" />}>
          <Plus data-icon="inline-start" />
          Nueva categoría
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
