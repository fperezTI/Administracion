import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getImportBatchById, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { formatDateTime, resolveTimeZone } from "@/lib/format-date";
import { IMPORT_BATCH_STATUS_LABELS, importBatchStatusBadgeVariant } from "@/lib/import-export-labels";
import { CommitImportBatchForm } from "./commit-import-batch-form";
import { cancelImportBatchAction } from "./actions";

const PRE_COMMIT_STATUSES = ["Queued", "Validating", "Validated"];
const IN_PROGRESS_STATUSES = ["Queued", "Validating", "Processing"];

export default async function ImportBatchDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let batch;
  try {
    batch = await getImportBatchById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  const me = await getMe(accessToken);
  const timeZone = resolveTimeZone(me.companies, batch.companyId);

  return (
    <>
      <AppHeader title="Importaciones" />
    <div className="mx-auto flex max-w-3xl flex-col gap-4 px-8 pb-8">
      <div className="mb-2 flex justify-end">
        <Button variant="outline" render={<Link href={`/imports?companyId=${batch.companyId}`} />}>
          ← Volver
        </Button>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold tracking-tight">{batch.fileName}</h2>
          <p className="text-muted-foreground text-sm">Subido el {formatDateTime(batch.createdAtUtc, timeZone)}</p>
        </div>
        <Badge variant={importBatchStatusBadgeVariant(batch.status)}>{IMPORT_BATCH_STATUS_LABELS[batch.status]}</Badge>
      </div>

      {IN_PROGRESS_STATUSES.includes(batch.status) && (
        <Card>
          <CardContent className="flex items-center justify-between gap-3 pt-6">
            <p className="text-muted-foreground text-sm">
              El lote se está procesando en segundo plano. Actualiza la página para ver el avance.
            </p>
            <Button variant="outline" size="sm" render={<Link href={`/imports/${id}`} />}>
              Actualizar
            </Button>
          </CardContent>
        </Card>
      )}

      {batch.errorMessage && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Error</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-destructive text-sm">{batch.errorMessage}</p>
          </CardContent>
        </Card>
      )}

      {(batch.totalRows !== null || batch.succeededRows !== null) && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Resumen</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">
            {batch.totalRows !== null && (
              <p>
                {batch.totalRows} filas totales — {batch.validRows} válidas, {batch.invalidRows} inválidas.
              </p>
            )}
            {batch.succeededRows !== null && (
              <p>
                {batch.succeededRows} activos creados, {batch.failedRows} filas no procesadas.
              </p>
            )}
          </CardContent>
        </Card>
      )}

      {batch.status === "Validated" && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Confirmar importación</CardTitle>
          </CardHeader>
          <CardContent>
            <CommitImportBatchForm importBatchId={batch.id} hasInvalidRows={(batch.invalidRows ?? 0) > 0} />
          </CardContent>
        </Card>
      )}

      {PRE_COMMIT_STATUSES.includes(batch.status) && (
        <form action={cancelImportBatchAction.bind(null, batch.id)}>
          <Button type="submit" variant="outline">
            Cancelar lote
          </Button>
        </form>
      )}

      {batch.rows.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">Detalle por fila</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Fila</TableHead>
                  <TableHead>Resultado</TableHead>
                  <TableHead>Detalle</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {batch.rows.map((row) => (
                  <TableRow key={row.rowNumber}>
                    <TableCell>{row.rowNumber}</TableCell>
                    <TableCell>
                      {row.success ? (
                        <span className="text-success">✓{row.createdAssetFolio && ` ${row.createdAssetFolio}`}</span>
                      ) : (
                        <span className="text-destructive">✗</span>
                      )}
                    </TableCell>
                    <TableCell className="text-muted-foreground text-xs">
                      {row.errors.length > 0 ? row.errors.join(" ") : "—"}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}
    </div>
    </>
  );
}
