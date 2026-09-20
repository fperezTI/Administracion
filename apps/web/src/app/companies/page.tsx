import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getCompanies } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { toggleCompanyActiveAction } from "./actions";

export default async function CompaniesPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const companies = await getCompanies(accessToken, { pageSize: 100 });
    content = (
      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Nombre comercial</TableHead>
              <TableHead>Razón social</TableHead>
              <TableHead>RFC / Id. fiscal</TableHead>
              <TableHead>Moneda</TableHead>
              <TableHead>Zona horaria</TableHead>
              <TableHead>Estado</TableHead>
              <TableHead />
            </TableRow>
          </TableHeader>
          <TableBody>
            {companies.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="text-muted-foreground py-8 text-center">
                  Todavía no hay empresas registradas.
                </TableCell>
              </TableRow>
            ) : (
              companies.items.map((company) => (
                <TableRow key={company.id}>
                  <TableCell className="font-medium">{company.tradeName}</TableCell>
                  <TableCell>{company.legalName}</TableCell>
                  <TableCell>{company.taxId}</TableCell>
                  <TableCell>{company.baseCurrency}</TableCell>
                  <TableCell>{company.timeZone}</TableCell>
                  <TableCell>
                    <Badge variant={company.isActive ? "success" : "outline"}>
                      {company.isActive ? "Activa" : "Inactiva"}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <form action={toggleCompanyActiveAction.bind(null, company.id, !company.isActive)}>
                      <Button type="submit" variant="outline" size="sm">
                        {company.isActive ? "Desactivar" : "Activar"}
                      </Button>
                    </form>
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
        {status === 403 ? "No tienes permiso para consultar empresas (Companies.Read)." : "No fue posible consultar las empresas."}
      </p>
    );
  }

  return (
    <div className="mx-auto max-w-4xl p-8">
      <AppHeader title="Empresas" subtitle="Hasta 50 empresas dentro de un único tenant de Entra ID." />
      <div className="mb-4 flex justify-end">
        <Button render={<Link href="/companies/new" />}>Nueva empresa</Button>
      </div>
      {content}
    </div>
  );
}
