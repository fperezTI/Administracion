import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getSpareParts, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { SPARE_PART_STATUS_LABELS, sparePartStatusBadgeVariant } from "@/lib/maintenance-labels";

export default async function SparePartsPage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
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
    const spareParts = await getSpareParts(accessToken, { companyId });

    content = (
      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Nombre</TableHead>
              <TableHead>Número de serie</TableHead>
              <TableHead>Estado</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {spareParts.length === 0 ? (
              <TableRow>
                <TableCell colSpan={3} className="text-muted-foreground py-8 text-center">
                  No hay refacciones registradas todavía.
                </TableCell>
              </TableRow>
            ) : (
              spareParts.map((p) => (
                <TableRow key={p.id}>
                  <TableCell>
                    <Link href={`/spare-parts/${p.id}?companyId=${companyId}`} className="hover:text-primary font-medium hover:underline">
                      {p.name}
                    </Link>
                    {p.partNumber && <span className="text-muted-foreground ml-1 text-xs">({p.partNumber})</span>}
                  </TableCell>
                  <TableCell className="font-mono">{p.serialNumber}</TableCell>
                  <TableCell>
                    <Badge variant={sparePartStatusBadgeVariant(p.status)}>{SPARE_PART_STATUS_LABELS[p.status]}</Badge>
                  </TableCell>
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
        {status === 403 ? "No tienes permiso para consultar refacciones (SpareParts.Read)." : "No fue posible consultar las refacciones."}
      </p>
    );
  }

  return (
    <div className="mx-auto max-w-3xl p-8">
      <AppHeader
        title="Refacciones"
        subtitle="Refacciones serializadas — historial de instalación y retiro por activo."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
      <div className="mb-4 flex justify-end">
        <Button render={<Link href={`/spare-parts/new?companyId=${companyId}`} />}>Nueva refacción</Button>
      </div>
      {content}
    </div>
  );
}
