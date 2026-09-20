import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getImportBatches, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { IMPORT_BATCH_STATUS_LABELS, importBatchStatusBadgeVariant } from "@/lib/import-export-labels";

export default async function ImportBatchesPage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);

  if (me.companies.length === 0) {
    return <EmptyCompanyState />;
  }

  const companyId = params.companyId && me.companies.some((c) => c.companyId === params.companyId)
    ? params.companyId
    : me.companies[0].companyId;

  let content: React.ReactNode;
  try {
    const batches = await getImportBatches(accessToken, { companyId, pageSize: 100 });

    content = (
      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Archivo</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead>Filas</TableHead>
              <TableHead>Subido</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {batches.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="text-muted-foreground py-8 text-center">
                  No hay lotes de importación todavía.
                </TableCell>
              </TableRow>
            ) : (
              batches.items.map((b) => (
                <TableRow key={b.id}>
                  <TableCell>
                    <Link href={`/imports/${b.id}`} className="hover:text-primary font-medium hover:underline">
                      {b.fileName}
                    </Link>
                  </TableCell>
                  <TableCell>
                    <Badge variant={importBatchStatusBadgeVariant(b.status)}>{IMPORT_BATCH_STATUS_LABELS[b.status]}</Badge>
                  </TableCell>
                  <TableCell className="text-muted-foreground text-sm">
                    {b.totalRows === null
                      ? "—"
                      : `${b.validRows ?? 0} válidas / ${b.invalidRows ?? 0} inválidas de ${b.totalRows}`}
                  </TableCell>
                  <TableCell>{new Date(b.createdAtUtc).toLocaleString("es-MX")}</TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar importaciones (Imports.Read)." : "No fue posible consultar los lotes de importación."}
      </p>
    );
  }

  return (
    <div className="mx-auto max-w-4xl p-8">
      <AppHeader
        title="Importaciones"
        subtitle="Carga masiva de activos por archivo CSV."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
      <div className="mb-4 flex justify-end">
        <Button render={<Link href={`/imports/new?companyId=${companyId}`} />}>Nueva importación</Button>
      </div>
      {content}
    </div>
  );
}
