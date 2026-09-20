import Link from "next/link";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getTemplates } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

export default async function TemplatesPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const templates = await getTemplates(accessToken);

    content = (
      <div className="rounded-lg border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Nombre</TableHead>
              <TableHead>Clave</TableHead>
              <TableHead>Versión actual</TableHead>
              <TableHead>Estado</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {templates.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="text-muted-foreground py-8 text-center">
                  No hay plantillas todavía.
                </TableCell>
              </TableRow>
            ) : (
              templates.map((t) => (
                <TableRow key={t.id}>
                  <TableCell>
                    <Link href={`/templates/${t.id}`} className="hover:text-primary font-medium hover:underline">
                      {t.name}
                    </Link>
                  </TableCell>
                  <TableCell className="font-mono">{t.key}</TableCell>
                  <TableCell>v{t.latestVersionNumber}</TableCell>
                  <TableCell>
                    <Badge variant={t.isActive ? "success" : "outline"}>{t.isActive ? "Activa" : "Inactiva"}</Badge>
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
        {status === 403 ? "No tienes permiso para consultar plantillas (Templates.Read)." : "No fue posible consultar las plantillas."}
      </p>
    );
  }

  return (
    <div className="mx-auto max-w-3xl p-8">
      <AppHeader
        title="Plantillas"
        subtitle="Catálogo versionado de texto (resguardos, correos, notificaciones) — sin generación de documentos todavía."
      />
      <div className="mb-4 flex justify-end">
        <Button render={<Link href="/templates/new" />}>Nueva plantilla</Button>
      </div>
      {content}
    </div>
  );
}
