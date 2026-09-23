import Link from "next/link";
import { Plus } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getTemplates, type TemplateSortField } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHeader, TableRow } from "@/components/ui/table";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 50;
const DEFAULT_SORT: TemplateSortField = "name";

export default async function TemplatesPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const pageNumber = params.pageNumber ? Math.max(1, Number(params.pageNumber)) : 1;
  const sortBy = (params.sortBy as TemplateSortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortDescending === "true";

  let content: React.ReactNode;
  try {
    const templates = await getTemplates(accessToken, { pageNumber, pageSize: PAGE_SIZE, sortBy, sortDescending });
    const totalPages = Math.max(1, Math.ceil(templates.totalCount / PAGE_SIZE));

    content = (
      <>
        <Table>
          <TableHeader>
            <TableRow>
              {[
                { key: "name", label: "Nombre" },
                { key: "key", label: "Clave" },
                { key: "latestVersionNumber", label: "Versión actual" },
                { key: "isActive", label: "Estado" },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/templates"
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
            {templates.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="text-muted-foreground py-8 text-center">
                  No hay plantillas todavía.
                </TableCell>
              </TableRow>
            ) : (
              templates.items.map((t) => (
                <TableRow key={t.id}>
                  <TableCell>
                    <Link href={`/templates/${t.id}`} className="hover:text-primary font-medium hover:underline">
                      {t.name}
                    </Link>
                  </TableCell>
                  <TableCell className="font-mono">{t.key}</TableCell>
                  <TableCell>v{t.latestVersionNumber}</TableCell>
                  <TableCell>
                    <Badge variant={t.isActive ? "success" : "outline"}>{t.isActive ? "Activa" : "Inactiva"}</Badge>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <TablePagination
          basePath="/templates"
          params={params}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={templates.totalCount}
          itemLabel="plantilla"
          itemLabelPlural="plantillas"
        />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar plantillas (Templates.Read)." : "No fue posible consultar las plantillas."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Plantillas"
        subtitle="Catálogo versionado de texto (resguardos, correos, notificaciones) — sin generación de documentos todavía."
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href="/templates/new" />}>
          <Plus data-icon="inline-start" />
          Nueva plantilla
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
