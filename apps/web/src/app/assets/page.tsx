import Link from "next/link";
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

const PAGE_SIZE = 20;

type SearchParams = Record<string, string | undefined>;

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
          <div className="flex flex-col gap-1">
            <Label htmlFor="assetCategoryId">Categoría</Label>
            <select
              id="assetCategoryId"
              name="assetCategoryId"
              defaultValue={params.assetCategoryId ?? ""}
              className="h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none dark:bg-input/30"
            >
              <option value="">Todas</option>
              {categories.items.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-1">
            <Label htmlFor="status">Estado</Label>
            <select
              id="status"
              name="status"
              defaultValue={params.status ?? ""}
              className="h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none dark:bg-input/30"
            >
              <option value="">Todos</option>
              {Object.entries(ASSET_STATUS_LABELS).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-1">
            <Label htmlFor="search">Buscar</Label>
            <Input id="search" name="search" defaultValue={params.search ?? ""} placeholder="Folio, marca, modelo, serie" />
          </div>
          <Button type="submit" variant="outline">
            Filtrar
          </Button>
        </form>

        <div className="rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Folio</TableHead>
                <TableHead>Categoría</TableHead>
                <TableHead>Marca / Modelo</TableHead>
                <TableHead>Serie</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead>Condición</TableHead>
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
                    <TableCell>{categoryNameById.get(asset.assetCategoryId) ?? "—"}</TableCell>
                    <TableCell>
                      {asset.brand} {asset.model}
                    </TableCell>
                    <TableCell>{asset.serialNumber ?? "—"}</TableCell>
                    <TableCell>
                      <Badge variant={assetStatusBadgeVariant(asset.status)}>{ASSET_STATUS_LABELS[asset.status]}</Badge>
                    </TableCell>
                    <TableCell>{PHYSICAL_CONDITION_LABELS[asset.physicalCondition]}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>

        <div className="mt-4 flex items-center justify-between text-sm">
          <p className="text-muted-foreground">
            {assetsResult.totalCount} activo{assetsResult.totalCount === 1 ? "" : "s"} — página {pageNumber} de{" "}
            {totalPages}
          </p>
          <div className="flex gap-2">
            <PageLink params={params} companyId={companyId} pageNumber={pageNumber - 1} disabled={pageNumber <= 1}>
              Anterior
            </PageLink>
            <PageLink
              params={params}
              companyId={companyId}
              pageNumber={pageNumber + 1}
              disabled={pageNumber >= totalPages}
            >
              Siguiente
            </PageLink>
          </div>
        </div>
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
    <div className="mx-auto max-w-5xl p-8">
      <AppHeader
        title="Activos"
        subtitle="Inventario de activos de TI por empresa."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
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
          Exportar PDF
        </Button>
        <Button render={<Link href="/assets/new" />}>Nuevo activo</Button>
      </div>
      {content}
    </div>
  );
}

function PageLink({
  params,
  companyId,
  pageNumber,
  disabled,
  children,
}: {
  params: SearchParams;
  companyId: string;
  pageNumber: number;
  disabled: boolean;
  children: React.ReactNode;
}) {
  if (disabled) {
    return (
      <span className="text-muted-foreground/50 rounded-lg border px-3 py-1">{children}</span>
    );
  }

  const query = new URLSearchParams({
    ...Object.fromEntries(Object.entries(params).filter(([, v]) => v !== undefined) as [string, string][]),
    companyId,
    pageNumber: String(pageNumber),
  });

  return (
    <Link href={`/assets?${query.toString()}`} className="rounded-lg border px-3 py-1 hover:bg-muted">
      {children}
    </Link>
  );
}
