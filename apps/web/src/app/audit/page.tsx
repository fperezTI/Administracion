import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAuditEntries, getCompanies, getMe, type AuditSortField } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Table, TableBody, TableCell, TableHeader, TableRow } from "@/components/ui/table";
import { formatDateTime, resolveTimeZone } from "@/lib/format-date";
import { AuditFilterForm } from "./audit-filter-form";
import { TablePagination, type SearchParams } from "@/components/layout/table-pagination";
import { SortableTableHead } from "@/components/layout/sortable-table-head";

const PAGE_SIZE = 50;
const DEFAULT_SORT: AuditSortField = "occurredAtUtc";

/** No CompanySwitcher — Audit.Read is a tenant-wide admin permission with no per-company membership
 * filter, same treatment as /companies (see the F8 plan and AppDbContext's remarks on AuditEntry). */
export default async function AuditPage({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);
  // Audit.Read can show entries for companies the viewer doesn't belong to — try the full company list
  // (needs Companies.Read too) for a per-row timezone, falling back to the viewer's own companies if that
  // permission isn't held, and "UTC" for rows with no companyId at all (tenant-wide actions).
  const allCompanies = await getCompanies(accessToken, { pageSize: 200 }).catch(() => null);
  const companiesForTimeZone = allCompanies?.items.map((c) => ({ companyId: c.id, timeZone: c.timeZone })) ?? me.companies;

  const pageNumber = params.pageNumber ? Math.max(1, Number(params.pageNumber)) : 1;
  // Sin sortBy en la URL, el default histórico del backend es occurredAtUtc descendente (más
  // reciente primero) sin importar sortDescending — reflejarlo aquí para que la flecha del
  // encabezado "Fecha" aparezca activa y apuntando hacia abajo desde la primera carga.
  const sortBy = (params.sortBy as AuditSortField | undefined) ?? DEFAULT_SORT;
  const sortDescending = params.sortBy === undefined ? true : params.sortDescending === "true";

  let content: React.ReactNode;
  try {
    const entries = await getAuditEntries(accessToken, {
      pageNumber,
      pageSize: PAGE_SIZE,
      commandName: params.commandName,
      fromUtc: params.fromUtc ? new Date(params.fromUtc).toISOString() : undefined,
      toUtc: params.toUtc ? new Date(params.toUtc).toISOString() : undefined,
      sortBy,
      sortDescending,
    });
    const totalPages = Math.max(1, Math.ceil(entries.totalCount / PAGE_SIZE));

    content = (
      <>
        <Table>
          <TableHeader>
            <TableRow>
              {[
                { key: "occurredAtUtc", label: "Fecha" },
                { key: "userDisplayName", label: "Usuario" },
                { key: "commandName", label: "Comando" },
                { key: "module", label: "Módulo.Acción" },
                { key: "succeeded", label: "Resultado" },
              ].map((column) => (
                <SortableTableHead
                  key={column.key}
                  basePath="/audit"
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
            {entries.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-muted-foreground py-8 text-center">
                  No hay entradas de auditoría todavía.
                </TableCell>
              </TableRow>
            ) : (
              entries.items.map((e) => (
                <TableRow key={e.id}>
                  <TableCell className="text-xs whitespace-nowrap">
                    {formatDateTime(e.occurredAtUtc, resolveTimeZone(companiesForTimeZone, e.companyId))}
                  </TableCell>
                  <TableCell>{e.userDisplayName ?? "—"}</TableCell>
                  <TableCell className="font-mono text-xs">{e.commandName}</TableCell>
                  <TableCell className="text-xs">{e.module && e.action ? `${e.module}.${e.action}` : "—"}</TableCell>
                  <TableCell>
                    <Badge variant={e.succeeded ? "success" : "destructive"}>{e.succeeded ? "Éxito" : "Error"}</Badge>
                    {!e.succeeded && e.errorMessage && <p className="text-destructive mt-1 text-xs">{e.errorMessage}</p>}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
        <TablePagination
          basePath="/audit"
          params={params}
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={entries.totalCount}
          itemLabel="entrada"
          itemLabelPlural="entradas"
        />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar la auditoría (Audit.Read)." : "No fue posible consultar la auditoría."}
      </p>
    );
  }

  return (
    <>
      <AppHeader title="Auditoría" subtitle="Registro de acciones sensibles ejecutadas en el sistema, éxito o fracaso." />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <AuditFilterForm
        defaultCommandName={params.commandName}
        defaultFromUtc={params.fromUtc}
        defaultToUtc={params.toUtc}
      />
      {content}
    </div>
    </>
  );
}
