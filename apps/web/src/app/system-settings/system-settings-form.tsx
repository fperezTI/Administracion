"use client";

import { useActionState } from "react";
import type { SystemSettings } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { updateSystemSettingsAction, type SystemSettingsFormState } from "./actions";

const initialState: SystemSettingsFormState = { error: null, success: false };

export function SystemSettingsForm({ settings }: { settings: SystemSettings }) {
  const [state, formAction, pending] = useActionState(updateSystemSettingsAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4 rounded-lg border p-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="senderMailbox">Buzón remitente</Label>
        <Input
          id="senderMailbox"
          name="senderMailbox"
          type="email"
          defaultValue={settings.senderMailbox}
          placeholder="notificaciones@tudominio.com"
          maxLength={320}
        />
        <p className="text-muted-foreground text-xs">
          Correo desde el cual se envían las notificaciones de asignación vía Microsoft Graph.
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="flex flex-col gap-1">
          <Label htmlFor="graphTenantId">Tenant ID (Microsoft Graph)</Label>
          <Input
            id="graphTenantId"
            name="graphTenantId"
            defaultValue={settings.graphTenantId}
            placeholder="00000000-0000-0000-0000-000000000000"
            maxLength={64}
          />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="graphClientId">Client ID (Microsoft Graph)</Label>
          <Input
            id="graphClientId"
            name="graphClientId"
            defaultValue={settings.graphClientId}
            placeholder="00000000-0000-0000-0000-000000000000"
            maxLength={64}
          />
        </div>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="graphClientSecret">Client Secret (Microsoft Graph)</Label>
        <Input
          id="graphClientSecret"
          name="graphClientSecret"
          type="password"
          placeholder="Dejar en blanco para no cambiar"
          maxLength={1000}
          autoComplete="off"
        />
        <p className="text-muted-foreground text-xs">
          {settings.hasGraphClientSecretConfigured ? "Secreto configurado." : "Secreto no configurado."} Nunca se
          muestra su valor — escribe uno nuevo solo si quieres reemplazarlo.
        </p>
      </div>

      <div className="flex items-center justify-between">
        <p className="text-sm" role="status">
          {state.error && <span className="text-destructive">{state.error}</span>}
          {!state.error && state.success && <span className="text-green-600 dark:text-green-500">Guardado.</span>}
        </p>
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Guardar"}
        </Button>
      </div>
    </form>
  );
}
