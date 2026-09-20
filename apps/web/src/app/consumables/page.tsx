import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getConsumables, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export default async function ConsumablesPage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
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
    const consumables = await getConsumables(accessToken, companyId);

    content = (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Nombre</TableHead>
              <TableHead>SKU</TableHead>
              <TableHead>Existencia</TableHead>
              <TableHead>Mínimo</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {consumables.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="text-muted-foreground py-8 text-center">
                  No hay consumibles registrados todavía.
                </TableCell>
              </TableRow>
            ) : (
              consumables.map((c) => {
                const belowMinimum = c.minimumStock !== null && c.currentStock < c.minimumStock;
                return (
                  <TableRow key={c.id}>
                    <TableCell>
                      <Link href={`/consumables/${c.id}?companyId=${companyId}`} className="hover:text-primary font-medium hover:underline">
                        {c.name}
                      </Link>
                    </TableCell>
                    <TableCell className="font-mono">{c.sku ?? "—"}</TableCell>
                    <TableCell>
                      {c.currentStock} {c.unitOfMeasure}
                      {belowMinimum && (
                        <Badge variant="destructive" className="ml-2">
                          Bajo mínimo
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell>{c.minimumStock ?? "—"}</TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar consumibles (Consumables.Read)." : "No fue posible consultar los consumibles."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Consumibles"
        subtitle="Existencia por consumible — cambia solo mediante movimientos registrados."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
    <div className="mx-auto max-w-6xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button render={<Link href={`/consumables/new?companyId=${companyId}`} />}>Nuevo consumible</Button>
      </div>
      {content}
    </div>
    </>
  );
}
