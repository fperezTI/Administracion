# Cómo crear los App Registrations de Microsoft Entra ID

Guía operativa para dar de alta las **dos** aplicaciones que el proyecto necesita en Entra ID. Es la
información externa marcada como pendiente en `docs/architecture/00-analysis.md` §18. Ninguno de estos pasos
lo puede ejecutar Claude Code: requieren acceso a tu portal/tenant de Azure. Una vez creados, los valores se
capturan como se indica en la sección final y el backend/frontend quedan conectados sin cambiar código.

Necesitas un rol con permiso para crear App Registrations en el tenant (built-in role **Cloud Application
Administrator** o **Application Administrator**, o ser dueño/colaborador con acceso a Microsoft Entra ID).

Se crean **dos** registros — uno por cada app, nunca comparten identidad:

| App Registration | Representa | Necesita secreto |
|---|---|---|
| `AssetManagement-API` | El backend ASP.NET Core (el recurso que expone datos) | No — solo valida tokens |
| `AssetManagement-Web` | El frontend Next.js (el cliente que pide tokens en nombre del usuario) | Sí — flujo confidencial (Route Handlers en servidor) |

Repite el proceso completo una vez por ambiente (`Dev`, `Test`, `Prod`) cuando llegues a desplegar en Azure
(sección `docs/deployment.md`) — para desarrollo local basta con uno.

## Opción A — Azure Portal (recomendada la primera vez)

### 1. Registrar la API

1. [portal.azure.com](https://portal.azure.com) → **Microsoft Entra ID** → **Registros de aplicaciones** →
   **Nuevo registro**.
2. Nombre: `AssetManagement-API-Dev`.
3. Tipos de cuenta admitidos: **Cuentas solo en este directorio organizativo (inquilino único)**.
4. URI de redirección: déjalo vacío (la API no inicia sesión interactiva).
5. **Registrar**.
6. En la página **Información general**, copia:
   - **Id. de aplicación (cliente)** → será `EntraId:ClientId` del API.
   - **Id. de directorio (inquilino)** → será `EntraId:TenantId` (compartido por ambas apps).
7. Ve a **Exponer una API**:
   - Junto a "URI de aplicación", clic **Establecer** → acepta el valor por defecto
     `api://<client-id>` → **Guardar**.
   - **Agregar un ámbito**:
     - Nombre del ámbito: **`access_as_user`** — escríbelo con cuidado, letra por letra. Un typo aquí
       (p. ej. `acces_as_user`) no da ningún error al guardar; falla hasta el login, con un mensaje
       `AADSTS650053: ... asked for scope 'access_as_user' that doesn't exist on the resource ...` que no
       apunta obviamente al nombre del scope.
     - Quién puede aceptar: **Administradores y usuarios**
     - Nombre para mostrar del consentimiento del administrador: `Acceder a la API de Gestión de Activos`
     - Descripción del consentimiento del administrador: `Permite a la aplicación acceder a la API de
       Gestión de Activos en nombre del usuario que inició sesión.`
     - Estado: **Habilitado** → **Agregar ámbito**.
8. Ve a **Configuración de tokens** (Token configuration) → **Agregar una notificación opcional** (Add
   optional claim) → tipo de token **Acceso** (no ID) → marca `email`, `given_name`, `family_name` (y
   `preferred_username` si aparece) → **Agregar**. El portal probablemente te pida confirmar activar el
   permiso de Microsoft Graph `email` asociado — acéptalo.
   - **Por qué**: por defecto, Entra ID solo incluye `email`/`name`/`preferred_username` en el **ID token**,
     no en el **access token** que se envía como `Authorization: Bearer` a una API personalizada. Sin este
     paso, `CurrentUserProvisioningMiddleware` no puede leer el correo del usuario desde el token y el
     aprovisionamiento falla con `DomainException: El correo del usuario es obligatorio.` (422). Si ya
     habías iniciado sesión antes de hacer este cambio, cierra sesión y vuelve a entrar — los tokens ya
     emitidos no lo incluyen retroactivamente.

No se necesita secreto ni permisos adicionales en este registro — solo valida los tokens que le llegan
(`Microsoft.Identity.Web` ya está configurado en `apps/api` para eso) — **salvo que quieras habilitar
"agregar usuario desde el directorio"** (buscar gente del tenant y darle rol/acceso a empresa antes de su
primer login), que sí necesita lo siguiente.

#### 1a. (Opcional) Habilitar la búsqueda del directorio (Microsoft Graph)

Reutiliza el mismo App Registration de la API — no crees uno nuevo:

1. En `AssetManagement-API-Dev` → **Permisos de API** → **Agregar un permiso** → **Microsoft Graph** →
   **Permisos de aplicación** (no delegados: la búsqueda la hace el backend por sí mismo, sin un usuario
   que haya iniciado sesión) → busca y marca **`User.Read.All`** → **Agregar permisos**.
2. Clic en **Conceder consentimiento de administrador para \<tu tenant\>** → **Sí**. Sin este paso, cada
   búsqueda falla con `403 Insufficient privileges to complete the operation.`
3. **Certificados y secretos** → **Nuevo secreto de cliente** → descripción + expiración (6–12 meses; en
   producción usar un certificado respaldado por Key Vault, igual que se recomienda para el frontend) →
   **Agregar** → copia el valor inmediatamente (no se vuelve a mostrar).
4. Backend (`apps/api`), vía user-secrets:
   ```bash
   dotnet user-secrets set "MicrosoftGraph:TenantId" "<tenant-id>" --project src/AssetManagement.Api
   dotnet user-secrets set "MicrosoftGraph:ClientId" "<api-client-id>" --project src/AssetManagement.Api
   dotnet user-secrets set "MicrosoftGraph:ClientSecret" "<el-secreto-que-copiaste>" --project src/AssetManagement.Api
   ```
5. Docker Compose: agrega `GRAPH_CLIENT_SECRET=<el-secreto-que-copiaste>` a tu `.env` (ya está conectado
   en `docker-compose.yml`, reutilizando `ENTRA_TENANT_ID`/`ENTRA_API_CLIENT_ID`).

Si dejas `MicrosoftGraph:ClientSecret` vacío, la funcionalidad queda desactivada de forma segura: el
endpoint de búsqueda responde con un error claro ("no configurada") en vez de fallar de forma confusa o
fingir que funciona.

#### 1b. (Opcional) Habilitar el correo de confirmación de asignación (Microsoft Graph `sendMail`)

Reutiliza el mismo App Registration y el mismo secreto de cliente del paso 1a — solo agrega un permiso más
y un buzón real. Necesitas una licencia de Microsoft 365 con un buzón dedicado (ej.
`notificaciones@tudominio.com`) desde el cual enviar.

1. En `AssetManagement-API-Dev` → **Permisos de API** → **Agregar un permiso** → **Microsoft Graph** →
   **Permisos de aplicación** → busca y marca **`Mail.Send`** → **Agregar permisos**.
2. Clic en **Conceder consentimiento de administrador para \<tu tenant\>** → **Sí**.
3. **(Recomendado, seguridad)** `Mail.Send` de aplicación permite enviar correo *como cualquier persona*
   del tenant si no se restringe. Limita la app a un solo buzón con una **Application Access Policy** de
   Exchange Online (PowerShell, una sola vez):
   ```powershell
   Connect-ExchangeOnline
   New-DistributionGroup -Name "AssetManagementSenders" -Members "notificaciones@tudominio.com" -Type Security
   New-ApplicationAccessPolicy -AppId "<api-client-id>" `
     -PolicyScopeGroupId "AssetManagementSenders@tudominio.com" `
     -AccessRight RestrictAccess -Description "Solo puede enviar correo desde el buzón de notificaciones"
   ```
4. Backend (`apps/api`), vía user-secrets (junto a los del paso 1a) — esto es el arranque inicial /
   "bootstrap"; ver el punto 6 para la forma recomendada de mantenerlo actualizado después:
   ```bash
   dotnet user-secrets set "MicrosoftGraph:SenderMailbox" "notificaciones@tudominio.com" --project src/AssetManagement.Api
   dotnet user-secrets set "Frontend:BaseUrl" "http://localhost:3000" --project src/AssetManagement.Api
   ```
5. Docker Compose: agrega `GRAPH_SENDER_MAILBOX=notificaciones@tudominio.com` a tu `.env`.
6. **Alternativa sin CLI/redeploy**: una vez que el paso 1a/1b esté hecho al menos una vez (permiso
   `Mail.Send` con consentimiento de administrador), el buzón remitente y las credenciales
   (`TenantId`/`ClientId`/`ClientSecret`) también se pueden configurar — y cambiar después — desde
   **Administración → Configuración** en la app (`/system-settings`, requiere el permiso
   `Configuration.Update`). El valor guardado ahí tiene prioridad sobre `appsettings`/variables de
   entorno campo por campo, y el cambio aplica de inmediato (sin redeploy) al próximo correo o búsqueda
   de directorio. El `ClientSecret` se guarda cifrado con el Data Protection API de ASP.NET Core, nunca
   en texto plano, y nunca se vuelve a mostrar una vez guardado. **Requisito de producción**: la clave de
   cifrado se persiste en el mismo Storage Account que ya se usa para documentos (contenedor
   `dataprotection-keys`, creado automáticamente) — si `BlobStorage:ConnectionString` no está configurado
   con un Storage Account real, el cifrado usa una clave local efímera y cualquier secreto guardado desde
   esta pantalla dejará de poder leerse la próxima vez que el contenedor de la API se reinicie. Recomendado
   como hardening adicional (no bloqueante): envolver esa clave con Key Vault
   (`ProtectKeysWithAzureKeyVault`), igual que ya se hace con la Application Access Policy de Exchange del
   punto 3.

Si dejas tanto `MicrosoftGraph:SenderMailbox` como el equivalente en la base de datos vacíos, no se manda
correo de asignación (cae en `Smtp:Host` si está configurado, o en un registro informativo si no hay
ningún proveedor de correo) — el resto de la app funciona igual.

### 2. Registrar el frontend (Web)

1. **Nuevo registro** otra vez.
2. Nombre: `AssetManagement-Web-Dev`.
3. Tipos de cuenta admitidos: **inquilino único** (igual que la API).
4. URI de redirección: tipo **Web**, valor `http://localhost:3000/api/auth/callback/microsoft-entra-id`
   (ruta estándar de NextAuth.js/Auth.js con proveedor Entra ID — ajusta si al implementar el login en
   `apps/web` se usa otra librería o ruta). Agrega también la URL de producción cuando exista
   (`docs/deployment.md`).
5. **Registrar**.
6. Copia el **Id. de aplicación (cliente)** → será `ENTRA_CLIENT_ID` del frontend.
7. **Certificados y secretos** → **Nuevo secreto de cliente** → descripción + expiración (6–12 meses;
   en producción usar un certificado respaldado por Key Vault en vez de un secreto de texto, ver
   `docs/deployment.md`) → **Agregar** → **copia el valor inmediatamente** (no se vuelve a mostrar).
8. **Permisos de API** → **Agregar un permiso** → pestaña **Mis API** → selecciona
   `AssetManagement-API-Dev` → **Permisos delegados** → marca `access_as_user` → **Agregar permisos**.
9. Clic en **Conceder consentimiento de administrador para \<tu tenant\>** → **Sí**. Sin este paso, cada
   usuario tendría que aceptar el consentimiento individualmente la primera vez que inicie sesión (no es
   incorrecto, pero para un app interno conviene concederlo una sola vez).
10. No actives "Tokens de id./acceso (concesión implícita)" en **Autenticación** — el flujo usado
    (Authorization Code + PKCE, cliente confidencial vía Route Handlers, ver
    `docs/security/authentication.md`) no lo necesita.

### 3. Valores que debes obtener al final

- **Tenant ID** (uno solo, compartido).
- **API Client ID** (Application ID URI = `api://<api-client-id>`).
- **Web Client ID**.
- **Web Client Secret** (solo se ve una vez — si lo pierdes, genera uno nuevo).

## Opción B — Azure CLI (para repetir el proceso o automatizarlo)

```bash
az login
az account set --subscription "<id-o-nombre-de-tu-suscripción>"

# --- API ---
az ad app create --display-name "AssetManagement-API-Dev" --sign-in-audience AzureADMyOrg
API_APP_ID=$(az ad app list --display-name "AssetManagement-API-Dev" --query "[0].appId" -o tsv)
az ad app update --id "$API_APP_ID" --identifier-uris "api://$API_APP_ID"

# Agregar el scope access_as_user (requiere un GUID nuevo para el id del scope)
SCOPE_ID=$(uuidgen 2>/dev/null || python3 -c "import uuid; print(uuid.uuid4())")
az ad app update --id "$API_APP_ID" --set api.oauth2PermissionScopes="[{
  \"id\": \"$SCOPE_ID\",
  \"adminConsentDescription\": \"Permite a la aplicación acceder a la API de Gestión de Activos en nombre del usuario.\",
  \"adminConsentDisplayName\": \"Acceder a la API de Gestión de Activos\",
  \"isEnabled\": true,
  \"type\": \"User\",
  \"userConsentDescription\": \"Permitir el acceso a la API de Gestión de Activos en tu nombre.\",
  \"userConsentDisplayName\": \"Acceder a la API de Gestión de Activos\",
  \"value\": \"access_as_user\"
}]"

# --- Web ---
az ad app create --display-name "AssetManagement-Web-Dev" --sign-in-audience AzureADMyOrg \
  --web-redirect-uris "http://localhost:3000/api/auth/callback/microsoft-entra-id"
WEB_APP_ID=$(az ad app list --display-name "AssetManagement-Web-Dev" --query "[0].appId" -o tsv)

# Secreto de cliente (guarda el valor que imprime — no se vuelve a mostrar)
az ad app credential reset --id "$WEB_APP_ID" --display-name "local-dev" --years 1

# Conceder el permiso delegado access_as_user hacia la API
az ad app permission add --id "$WEB_APP_ID" --api "$API_APP_ID" \
  --api-permissions "${SCOPE_ID}=Scope"
az ad app permission grant --id "$WEB_APP_ID" --api "$API_APP_ID"
az ad app permission admin-consent --id "$WEB_APP_ID"

TENANT_ID=$(az account show --query tenantId -o tsv)
echo "TenantId=$TENANT_ID"
echo "ApiClientId=$API_APP_ID"
echo "WebClientId=$WEB_APP_ID"
```

La creación del *service principal* asociado a cada app (`az ad sp create --id <appId>`) suele hacerla Azure
automáticamente al conceder permisos/consentimiento; si algún paso reclama que no existe, créalo
explícitamente con `az ad sp create --id "$API_APP_ID"` (y lo mismo para `$WEB_APP_ID`) antes de continuar.

## 4. Conectar los valores al proyecto (nunca al repositorio)

**Backend** (`apps/api`) — usa *user-secrets* en local, nunca el `appsettings.json` versionado:

```bash
cd apps/api
dotnet user-secrets init --project src/AssetManagement.Api
dotnet user-secrets set "EntraId:Instance" "https://login.microsoftonline.com/" --project src/AssetManagement.Api
dotnet user-secrets set "EntraId:TenantId" "<tenant-id>" --project src/AssetManagement.Api
dotnet user-secrets set "EntraId:ClientId" "<api-client-id>" --project src/AssetManagement.Api
```

**Frontend** (`apps/web`) — copia `.env.example` a `.env.local` y complétalo:

```
ENTRA_TENANT_ID=<tenant-id>
ENTRA_CLIENT_ID=<web-client-id>
ENTRA_CLIENT_SECRET=<web-client-secret>
ENTRA_API_CLIENT_ID=<api-client-id>          # para pedir el scope api://<api-client-id>/access_as_user
AUTH_SECRET=<genera con el comando en .env.example>
AUTH_URL=http://localhost:3000               # la URL pública real en cada ambiente
```

**Docker Compose** — ya está conectado en `docker-compose.yml` (servicios `api` y `web`), leyendo de tu `.env`
en la raíz del repo (ya `.gitignore`d) las claves `ENTRA_TENANT_ID`, `ENTRA_API_CLIENT_ID`,
`ENTRA_WEB_CLIENT_ID`, `ENTRA_WEB_CLIENT_SECRET` y `AUTH_SECRET` — cópialas de `.env.example` y complétalas
igual que `SQLSERVER_SA_PASSWORD`.

**Azure (cuando llegues a F13)** — estos mismos valores van a Key Vault y se referencian desde App Service
(`@Microsoft.KeyVault(...)`), nunca como texto plano en Bicep ni en variables de entorno del pipeline. El
`ClientSecret` del frontend en particular debe rotarse antes de su expiración (recordatorio: quedó anotado
en `docs/operations.md` como runbook pendiente de completar en F13).

## Verificación

Con `docker compose up -d --build` (o el backend/frontend corriendo en modo desarrollo), abre
http://localhost:3000, clic en "Iniciar sesión con Microsoft" y completa el login real (incluido MFA si tu
política de Conditional Access lo exige). Debe aterrizar en `/dashboard` mostrando tu perfil, permisos
(vacíos hasta que te asignen un rol) y empresas — es la señal de que `GET /api/v1/me` respondió `200` con un
token real, en vez del `401` que verás mientras estos valores sean placeholders.

### Problemas ya encontrados y resueltos en este proyecto (por si se repiten en otro ambiente)

| Síntoma | Causa | Solución |
|---|---|---|
| Tras el login, redirige a una pantalla de error con `signin?error=OAuthCallbackError` | El nombre del scope tiene un typo (p. ej. `acces_as_user` en vez de `access_as_user`) | Corrige el nombre exacto del scope en "Exponer una API" del registro de la API |
| El dashboard muestra "No fue posible consultar tu perfil en el API" y el log del API dice `DomainException: El correo del usuario es obligatorio` | El access token no trae el claim `email` (Entra solo lo incluye en el ID token por defecto) | Agrega `email` como notificación opcional del **token de acceso** (paso 8 más arriba) y vuelve a iniciar sesión |
