import Link from "next/link";
import { FileSpreadsheet, FileText, Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssetCategories, getAssets, getAssetsExportUrl, getMe, type AssetStatus } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { ASSET_STATUS_LABELS, PHYSICAL_CONDITION_LABELS, assetStatusBadgeVariant } from "@/lib/asset-labels";
import { AssetFilterFields } from "./asset-filter-fields";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";

const PAGE_SIZE = 20;

export default async function AssetsPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);

  if (me.companies.length === 0) {
    return <EmptyCompanyState />;
  }

  const companyId = params.companyId && me.companies.some((c) => c.companyId === params.companyId)
    ? params.companyId
    : me.companies[0].companyId;
  const pageNumber = params.pageNumber ? Math.max(1, Number(params.pageNumber)) : 1;

  let content: React.ReactNode;
  try {
    const [assetsResult, categories] = await Promise.all([
      getAssets(accessToken, {
        companyId,
        pageNumber,
        pageSize: PAGE_SIZE,
        assetCategoryId: params.assetCategoryId,
        status: params.status as AssetStatus | undefined,
        search: params.search,
      }),
      getAssetCategories(accessToken, { pageSize: 200 }),
    ]);

    const categoryNameById = new Map(categories.items.map((c) => [c.id, c.name]));
    const totalPages = Math.max(1, Math.ceil(assetsResult.totalCount / PAGE_SIZE));

    content = (
      <>
        <form method="GET" className="mb-4 flex flex-wrap items-end gap-3">
          <input type="hidden" name="companyId" value={companyId} />
          <AssetFilterFields
            categories={categories.items}
            defaultCategoryId={params.assetCategoryId ?? ""}
            defaultStatus={params.status ?? ""}
          />
          <div className="flex flex-col gap-1">
            <Label htmlFor="search">Buscar</Label>
            <Input id="search" name="search" defaultValue={params.search ?? ""} placeholder="Folio, marca, modelo, serie" />
          </div>
          <Button type="submit" variant="outline">
            Filtrar
          </Button>
        </form>

          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Folio</TableHead>
                <TableHead className="hidden sm:table-cell">Categoría</TableHead>
                <TableHead>Marca / Modelo</TableHead>
                <TableHead className="hidden sm:table-cell">Serie</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead className="hidden sm:table-cell">Condición</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {assetsResult.items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-muted-foreground py-8 text-center">
                    No se encontraron activos con estos filtros.
                  </TableCell>
                </TableRow>
              ) : (
                assetsResult.items.map((asset) => (
                  <TableRow key={asset.id}>
                    <TableCell>
                      <Link href={`/assets/${asset.id}`} className="hover:text-primary font-mono font-medium hover:underline">
                        {asset.internalFolio}
                      </Link>
                    </TableCell>
                    <TableCell className="hidden sm:table-cell">{categoryNameById.get(asset.assetCategoryId) ?? "—"}</TableCell>
                    <TableCell>
                      {asset.brand} {asset.model}
                    </TableCell>
                    <TableCell className="hidden sm:table-cell">{asset.serialNumber ?? "—"}</TableCell>
                    <TableCell>
                      <Badge variant={assetStatusBadgeVariant(asset.status)}>{ASSET_STATUS_LABELS[asset.status]}</Badge>
                    </TableCell>
                    <TableCell className="hidden sm:table-cell">{PHYSICAL_CONDITION_LABELS[asset.physicalCondition]}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>

        <TablePagination
          basePath="/assets"
          params={params}
          companyId={companyId}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={assetsResult.totalCount}
          itemLabel="activo"
          itemLabelPlural="activos"
        />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar activos (Assets.Read)." : "No fue posible consultar los activos."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Activos"
        subtitle="Inventario de activos de TI por empresa."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex items-center justify-end gap-3">
        <Button
          variant="outline"
          render={
            <a
              href={getAssetsExportUrl({
                companyId,
                format: "Xlsx",
                assetCategoryId: params.assetCategoryId,
                status: params.status as AssetStatus | undefined,
                search: params.search,
              })}
            />
          }
        >
          <FileSpreadsheet data-icon="inline-start" />
          Exportar Excel
        </Button>
        <Button
          variant="outline"
          render={
            <a
              href={getAssetsExportUrl({
                companyId,
                format: "Pdf",
                assetCategoryId: params.assetCategoryId,
                status: params.status as AssetStatus | undefined,
                search: params.search,
              })}
            />
          }
        >
          <FileText data-icon="inline-start" />
          Exportar PDF
        </Button>
        <Button render={<Link href="/assets/new" />}>
          <Plus data-icon="inline-start" />
          Nuevo activo
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
