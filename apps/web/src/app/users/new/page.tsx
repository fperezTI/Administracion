import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getCompanies, getRoles, searchDirectoryUsers } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { CreateUserForm } from "./create-user-form";

export default async function NewUserFromDirectoryPage({
  searchParams,
}: {
  searchParams: Promise<{ query?: string; entraObjectId?: string; displayName?: string; email?: string }>;
}) {
  const accessToken = await requireAccessToken();
  const params = await searchParams;

  const selected =
    params.entraObjectId && params.displayName && params.email
      ? { entraObjectId: params.entraObjectId, displayName: params.displayName, email: params.email }
      : null;

  let content: React.ReactNode;

  if (selected) {
    const [roles, companies] = await Promise.all([
      getRoles(accessToken, { pageSize: 200, isActive: true }),
      getCompanies(accessToken, { pageSize: 200 }),
    ]);

    content = (
      <div className="flex flex-col gap-4">
        <div className="rounded-lg border p-4 text-sm">
          <p className="font-medium">{selected.displayName}</p>
          <p className="text-muted-foreground">{selected.email}</p>
        </div>
        <CreateUserForm
          entraObjectId={selected.entraObjectId}
          displayName={selected.displayName}
          email={selected.email}
          roles={roles.items}
          companies={companies.items}
        />
        <Button variant="outline" render={<Link href="/users/new" />} className="self-start">
          <ArrowLeft data-icon="inline-start" />
          Buscar a alguien más
        </Button>
      </div>
    );
  } else {
    let results: Awaited<ReturnType<typeof searchDirectoryUsers>> = [];
    let searchError: string | null = null;
    if (params.query) {
      try {
        results = await searchDirectoryUsers(accessToken, params.query);
      } catch (error) {
        searchError =
          error instanceof ApiError
            ? (error.detail ?? "No fue posible consultar el directorio.")
            : "No fue posible consultar el directorio.";
      }
    }

    content = (
      <div className="flex flex-col gap-4">
        <form method="GET" className="flex items-end gap-3">
          <div className="flex flex-1 flex-col gap-1">
            <Label htmlFor="query">Nombre o correo</Label>
            <Input id="query" name="query" defaultValue={params.query ?? ""} placeholder="Buscar en el directorio de Entra ID" />
          </div>
          <Button type="submit" variant="outline">
            Buscar
          </Button>
        </form>

        {searchError && <p className="text-destructive text-sm">{searchError}</p>}

        {params.query && !searchError && (
          <div className="flex flex-col gap-2">
            {results.length === 0 ? (
              <p className="text-muted-foreground text-sm">
                Sin resultados (o ya tienen perfil en el sistema).
              </p>
            ) : (
              results.map((user) => (
                <div key={user.entraObjectId} className="flex items-center justify-between rounded-lg border p-3 text-sm">
                  <div>
                    <p className="font-medium">{user.displayName}</p>
                    <p className="text-muted-foreground">{user.email}</p>
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    render={
                      <Link
                        href={`/users/new?query=${encodeURIComponent(params.query ?? "")}&entraObjectId=${user.entraObjectId}&displayName=${encodeURIComponent(user.displayName)}&email=${encodeURIComponent(user.email)}`}
                      />
                    }
                  >
                    Seleccionar
                  </Button>
                </div>
              ))
            )}
          </div>
        )}
      </div>
    );
  }

  return (
    <>
      <AppHeader
        title="Agregar usuario"
        subtitle="Busca a alguien que ya exista en el directorio de Entra ID para darle acceso antes de su primer inicio de sesión."
      />
    <div className="mx-auto max-w-xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href="/users" />}>
          <ArrowLeft data-icon="inline-start" />
          Volver
        </Button>
      </div>
      {content}
    </div>
    </>
  );
}
