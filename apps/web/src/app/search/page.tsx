import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, globalSearch, getMe } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { SEARCH_ENTITY_TYPE_LABELS, searchResultHref } from "@/lib/search-labels";

export default async function SearchPage({
  searchParams,
}: {
  searchParams: Promise<{ term?: string; companyId?: string }>;
}) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;
  const me = await getMe(accessToken);

  const term = (params.term ?? "").trim();
  const companyId = params.companyId && me.companies.some((c) => c.companyId === params.companyId) ? params.companyId : undefined;

  let content: React.ReactNode;
  if (term.length < 2) {
    content = <p className="text-muted-foreground text-sm">Escribe al menos 2 caracteres para buscar.</p>;
  } else {
    try {
      const results = await globalSearch(accessToken, { term, companyId });

      if (results.length === 0) {
        content = <p className="text-muted-foreground text-sm">Sin resultados para &quot;{term}&quot;.</p>;
      } else {
        const groups = new Map<string, typeof results>();
        for (const result of results) {
          const group = groups.get(result.entityType);
          if (group) {
            group.push(result);
          } else {
            groups.set(result.entityType, [result]);
          }
        }

        content = (
          <div className="flex flex-col gap-4">
            {[...groups.entries()].map(([entityType, items]) => (
              <Card key={entityType}>
                <CardHeader>
                  <CardTitle className="text-sm">{SEARCH_ENTITY_TYPE_LABELS[entityType] ?? entityType}</CardTitle>
                </CardHeader>
                <CardContent className="flex flex-col gap-1">
                  {items.map((item) => (
                    <Link
                      key={`${item.entityType}-${item.entityId}`}
                      href={searchResultHref(item.linkEntityType, item.linkEntityId)}
                      className="hover:bg-muted flex flex-col rounded-md px-2 py-1.5 text-sm"
                    >
                      <span className="font-medium">{item.title}</span>
                      {item.subtitle && <span className="text-muted-foreground text-xs">{item.subtitle}</span>}
                    </Link>
                  ))}
                </CardContent>
              </Card>
            ))}
          </div>
        );
      }
    } catch (error) {
      const status = error instanceof ApiError ? error.status : undefined;
      content = (
        <p className="text-destructive text-sm">
          {status === 403 ? "Se requiere iniciar sesión para buscar." : "No fue posible completar la búsqueda."}
        </p>
      );
    }
  }

  return (
    <div className="mx-auto max-w-3xl p-8">
      <AppHeader
        title="Búsqueda"
        subtitle="Resultados en activos, movimientos, mantenimiento, garantías, refacciones, consumibles, solicitudes y usuarios."
      />

      <form method="GET" className="mb-4 flex flex-wrap items-end gap-3">
        <div className="flex flex-1 flex-col gap-1">
          <Input name="term" defaultValue={params.term ?? ""} placeholder="Folio, marca, modelo, serie, proveedor, nombre…" autoFocus />
        </div>
        {companyId && <input type="hidden" name="companyId" value={companyId} />}
        <Button type="submit">Buscar</Button>
      </form>

      <div className="mb-4">
        {companyId ? (
          <CompanySwitcher companies={me.companies} currentCompanyId={companyId} />
        ) : (
          me.companies.length > 0 && (
            <Link href={`/search?term=${encodeURIComponent(params.term ?? "")}&companyId=${me.companies[0].companyId}`} className="text-primary text-sm hover:underline">
              Acotar a una empresa
            </Link>
          )
        )}
      </div>

      {content}
    </div>
  );
}
