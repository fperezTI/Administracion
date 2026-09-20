import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getMyNotifications } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { markAsReadAction } from "./actions";

export default async function NotificationsPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const notifications = await getMyNotifications(accessToken);

    content =
      notifications.length === 0 ? (
        <p className="text-muted-foreground text-sm">No tienes notificaciones todavía.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {notifications.map((n) => (
            <Card key={n.id}>
              <CardContent className="flex items-start justify-between gap-3">
                <div>
                  <div className="mb-1 flex items-center gap-2">
                    <p className="font-medium">{n.title}</p>
                    {!n.isRead && <Badge variant="warning">Nueva</Badge>}
                  </div>
                  <p className="text-muted-foreground text-sm">{n.body}</p>
                  <p className="text-muted-foreground mt-1 text-xs">{new Date(n.createdAtUtc).toLocaleString("es-MX")}</p>
                </div>
                {!n.isRead && (
                  <form action={markAsReadAction.bind(null, n.id)}>
                    <Button type="submit" size="sm" variant="outline">
                      Marcar como leída
                    </Button>
                  </form>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No fue posible consultar tus notificaciones." : "No fue posible consultar tus notificaciones."}
      </p>
    );
  }

  return (
    <>
      <AppHeader title="Mis notificaciones" subtitle="Aprobaciones pendientes y resultados de tus propias solicitudes." />
    <div className="mx-auto max-w-3xl px-8 pb-8">
      {content}
    </div>
    </>
  );
}
