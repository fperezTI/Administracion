import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getSystemSettings } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { SystemSettingsForm } from "./system-settings-form";

export default async function SystemSettingsPage() {
  const accessToken = await requireAccessToken();

  let content: React.ReactNode;
  try {
    const settings = await getSystemSettings(accessToken);
    content = <SystemSettingsForm settings={settings} />;
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403
          ? "No tienes permiso para consultar la configuración del sistema (Configuration.Read)."
          : "No fue posible consultar la configuración del sistema."}
      </p>
    );
  }

  return (
    <>
      <AppHeader
        title="Configuración"
        subtitle="Buzón remitente y credenciales de Microsoft Graph usados para notificaciones y directorio."
      />
      <div className="mx-auto max-w-2xl px-8 pb-8">{content}</div>
    </>
  );
}
