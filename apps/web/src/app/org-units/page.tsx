import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMe, getOrgUnitTree, getOrgUnitTypes } from "@/lib/api";
import { flattenOrgUnitTree } from "@/lib/org-unit-tree";
import { AppHeader } from "@/components/app-header";
import { CompanySwitcher } from "@/components/company-switcher";
import { EmptyCompanyState } from "@/components/empty-company-state";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { CreateOrgUnitForm } from "./create-org-unit-form";
import { moveOrgUnitAction, toggleOrgUnitActiveAction } from "./actions";

const selectClassName =
  "h-7 rounded-lg border border-input bg-transparent px-2 text-xs outline-none dark:bg-input/30";

export default async function OrgUnitsPage({ searchParams }: { searchParams: Promise<{ companyId?: string }> }) {
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
    const [tree, types] = await Promise.all([getOrgUnitTree(accessToken, companyId), getOrgUnitTypes(accessToken)]);
    const options = flattenOrgUnitTree(tree);
    const optionsById = new Map(options.map((o) => [o.id, o]));

    content = (
      <>
        <div className="rounded-lg border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Nombre</TableHead>
                <TableHead>Tipo</TableHead>
                <TableHead>Código</TableHead>
                <TableHead>Estado</TableHead>
                <TableHead>Mover a</TableHead>
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {tree.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-muted-foreground py-8 text-center">
                    Esta empresa todavía no tiene estructura organizacional.
                  </TableCell>
                </TableRow>
              ) : (
                options.map((option) => {
                  const node = tree.find((n) => n.id === option.id)!;
                  return (
                    <TableRow key={node.id}>
                      <TableCell className="font-medium">{option.label}</TableCell>
                      <TableCell>{node.orgUnitTypeName}</TableCell>
                      <TableCell className="font-mono text-xs">{node.code}</TableCell>
                      <TableCell>
                        <Badge variant={node.isActive ? "success" : "outline"}>
                          {node.isActive ? "Activa" : "Inactiva"}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <form action={moveOrgUnitAction.bind(null, node.id)} className="flex items-center gap-1">
                          <select
                            name={`newParent:${node.id}`}
                            defaultValue={node.parentOrgUnitId ?? ""}
                            className={selectClassName}
                          >
                            <option value="">Ninguna (raíz)</option>
                            {options
                              .filter((o) => o.id !== node.id)
                              .map((o) => (
                                <option key={o.id} value={o.id}>
                                  {optionsById.get(o.id)?.label}
                                </option>
                              ))}
                          </select>
                          <Button type="submit" variant="outline" size="xs">
                            Mover
                          </Button>
                        </form>
                      </TableCell>
                      <TableCell>
                        <form action={toggleOrgUnitActiveAction.bind(null, node.id, !node.isActive)}>
                          <Button type="submit" variant="outline" size="sm">
                            {node.isActive ? "Desactivar" : "Activar"}
                          </Button>
                        </form>
                      </TableCell>
                    </TableRow>
                  );
                })
              )}
            </TableBody>
          </Table>
        </div>

        <CreateOrgUnitForm companyId={companyId} types={types} orgUnitOptions={options} />
      </>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403
          ? "No tienes permiso para consultar la estructura organizacional (Structure.Read)."
          : "No fue posible consultar la estructura organizacional."}
      </p>
    );
  }

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-4 p-8">
      <AppHeader
        title="Estructura organizacional"
        subtitle="Jerarquía de unidades organizacionales por empresa."
        activeCompany={<CompanySwitcher companies={me.companies} currentCompanyId={companyId} />}
      />
      {content}
    </div>
  );
}
