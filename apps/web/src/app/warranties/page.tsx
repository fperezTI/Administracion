import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getWarranties, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { WARRANTY_TYPE_LABELS } from "@/lib/maintenance-labels";

export default async function WarrantiesPage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
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
    const warranties = await getWarranties(accessToken, { companyId });
    const today = new Date().toISOString().slice(0, 10);

    content = (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Activo</TableHead>
              <TableHead>Tipo</TableHead>
              <TableHead>Proveedor</TableHead>
              <TableHead>Vigencia</TableHead>
              <TableHead />
            </TableRow>
          </TableHeader>
          <TableBody>
            {warranties.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="text-muted-foreground py-8 text-center">
                  No hay garantías registradas todavía.
                </TableCell>
              </TableRow>
            ) : (
              warranties.map((w) => (
                <TableRow key={w.id}>
                  <TableCell className="font-mono">{w.assetFolio}</TableCell>
                  <TableCell>{WARRANTY_TYPE_LABELS[w.type]}</TableCell>
                  <TableCell>{w.provider}</TableCell>
                  <TableCell className={w.endDate < today ? "text-destructive" : undefined}>
                    {w.startDate} — {w.endDate}
                  </TableCell>
                  <TableCell>
                    <Link href={`/warranties/${w.id}?companyId=${companyId}`} className="hover:text-primary text-sm hover:underline">
                      Editar
                    </Link>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar garantías (Warranties.Read)." : "No fue posible consultar las garantías."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Garantías"
        subtitle="Coberturas de garantía o soporte registradas por activo."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href={`/warranties/new?companyId=${companyId}`} />}>Nueva garantía</Button>
      </div>
      {content}
    </div>
    </>
  );
}
