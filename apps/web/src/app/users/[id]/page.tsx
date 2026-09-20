import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getCompanies, getRoles, getUserById } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { anonymizeUserAction, assignRoleAction, grantCompanyAction, removeRoleAction, revokeCompanyAction } from "./actions";

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none dark:bg-input/30";

export default async function UserDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let user;
  try {
    user = await getUserById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  const [allRoles, allCompanies] = await Promise.all([
    getRoles(accessToken, { pageSize: 200, isActive: true }),
    getCompanies(accessToken, { pageSize: 200 }),
  ]);

  const roleNameById = new Map(allRoles.items.map((r) => [r.id, r.name]));
  const companyNameById = new Map(allCompanies.items.map((c) => [c.id, c.tradeName]));
  const assignableRoles = allRoles.items.filter((r) => !user.roleIds.includes(r.id));
  const grantableCompanies = allCompanies.items.filter((c) => !user.companyIds.includes(c.id));

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4 p-8">
      <AppHeader title="Usuarios" />
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold tracking-tight">{user.displayName}</h2>
          <p className="text-muted-foreground text-sm">{user.email}</p>
        </div>
        <Badge variant={user.isActive ? "success" : "outline"}>{user.isActive ? "Activo" : "Inactivo"}</Badge>
      </div>

      <section className="flex flex-col gap-2 rounded-lg border p-4">
        <p className="text-sm font-medium">Roles asignados</p>
        {user.roleIds.length === 0 ? (
          <p className="text-muted-foreground text-sm">Sin roles asignados.</p>
        ) : (
          <ul className="flex flex-col gap-1">
            {user.roleIds.map((roleId) => (
              <li key={roleId} className="flex items-center justify-between text-sm">
                <span>{roleNameById.get(roleId) ?? roleId}</span>
                <form action={removeRoleAction.bind(null, id, roleId)}>
                  <Button type="submit" variant="outline" size="xs">
                    Quitar
                  </Button>
                </form>
              </li>
            ))}
          </ul>
        )}
        {assignableRoles.length > 0 && (
          <form action={assignRoleAction.bind(null, id)} className="mt-2 flex items-center gap-2">
            <select name="roleId" className={selectClassName} defaultValue="" required>
              <option value="" disabled>
                Selecciona un rol
              </option>
              {assignableRoles.map((role) => (
                <option key={role.id} value={role.id}>
                  {role.name}
                </option>
              ))}
            </select>
            <Button type="submit" variant="outline" size="sm">
              Asignar
            </Button>
          </form>
        )}
      </section>

      <section className="flex flex-col gap-2 rounded-lg border p-4">
        <p className="text-sm font-medium">Empresas con acceso</p>
        {user.companyIds.length === 0 ? (
          <p className="text-muted-foreground text-sm">Sin acceso a ninguna empresa.</p>
        ) : (
          <ul className="flex flex-col gap-1">
            {user.companyIds.map((companyId) => (
              <li key={companyId} className="flex items-center justify-between text-sm">
                <span>{companyNameById.get(companyId) ?? companyId}</span>
                <form action={revokeCompanyAction.bind(null, id, companyId)}>
                  <Button type="submit" variant="outline" size="xs">
                    Revocar
                  </Button>
                </form>
              </li>
            ))}
          </ul>
        )}
        {grantableCompanies.length > 0 && (
          <form action={grantCompanyAction.bind(null, id)} className="mt-2 flex items-center gap-2">
            <select name="companyId" className={selectClassName} defaultValue="" required>
              <option value="" disabled>
                Selecciona una empresa
              </option>
              {grantableCompanies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.tradeName}
                </option>
              ))}
            </select>
            <Button type="submit" variant="outline" size="sm">
              Otorgar
            </Button>
          </form>
        )}
      </section>

      <section className="flex flex-col gap-2 rounded-lg border p-4">
        <p className="text-sm font-medium">Privacidad</p>
        {user.anonymizedAtUtc ? (
          <p className="text-muted-foreground text-sm">
            Anonimizado el {new Date(user.anonymizedAtUtc).toLocaleString("es-MX")}. El nombre y correo reales ya no están disponibles.
          </p>
        ) : (
          <>
            <p className="text-muted-foreground text-sm">
              Elimina permanentemente el nombre y correo reales de este perfil (ver{" "}
              <span className="font-mono">docs/privacy-retention.md</span>). No afecta el historial de auditoría ya
              registrado. Esta acción no se puede deshacer.
            </p>
            <form action={anonymizeUserAction.bind(null, id)}>
              <Button type="submit" variant="destructive" size="sm">
                Anonimizar (irreversible)
              </Button>
            </form>
          </>
        )}
      </section>

      <Button variant="outline" render={<Link href="/users" />} className="self-start">
        ← Volver
      </Button>
    </div>
  );
}
