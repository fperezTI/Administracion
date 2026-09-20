import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getTemplateById } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { AddVersionForm } from "./add-version-form";
import { toggleTemplateActiveAction } from "./actions";

export default async function TemplateDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let template;
  try {
    template = await getTemplateById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4 p-8">
      <AppHeader title="Plantillas" />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h2 className="text-lg font-semibold tracking-tight">{template.name}</h2>
          <p className="text-muted-foreground font-mono text-sm">{template.key}</p>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={template.isActive ? "success" : "outline"}>{template.isActive ? "Activa" : "Inactiva"}</Badge>
          <form action={toggleTemplateActiveAction.bind(null, template.id, !template.isActive)}>
            <Button variant="outline" size="sm" type="submit">
              {template.isActive ? "Desactivar" : "Activar"}
            </Button>
          </form>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">Agregar nueva versión</CardTitle>
        </CardHeader>
        <CardContent>
          <AddVersionForm templateId={template.id} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">Historial de versiones</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {template.versions.map((v) => (
            <div key={v.versionNumber} className="border-b pb-3 last:border-b-0 last:pb-0">
              <p className="text-muted-foreground mb-1 text-xs">
                v{v.versionNumber} — {new Date(v.createdAtUtc).toLocaleString("es-MX")}
              </p>
              <p className="text-sm whitespace-pre-wrap">{v.content}</p>
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  );
}
