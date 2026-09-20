# Autenticación

Único proveedor de identidad: **Microsoft Entra ID**. No hay cuentas locales, ni contraseñas, ni recuperación
de contraseña gestionadas por la aplicación.

## Flujo (implementado y verificado contra un tenant real)

1. El usuario abre `apps/web`. Si no hay sesión, la página de inicio muestra "Iniciar sesión con Microsoft"
   (`src/app/page.tsx`), que dispara un *server action* (`signIn("microsoft-entra-id", { redirectTo:
   "/dashboard" })`) — ejecutado enteramente en el servidor, ninguna librería de auth llega al bundle del
   cliente.
2. **NextAuth (Auth.js) v5** (`src/auth.ts`) maneja **Authorization Code + PKCE** contra Entra ID: genera el
   `code_verifier`/`code_challenge`, lo guarda en una cookie `httpOnly` de corta vida
   (`authjs.pkce.code_verifier`) y redirige al navegador a
   `https://login.microsoftonline.com/<tenant>/oauth2/v2.0/authorize` pidiendo el scope
   `openid profile email offline_access api://<api-client-id>/access_as_user` — es decir, un token ya
   audience-scoped a nuestra propia API, no solo a Microsoft Graph.
3. Tras el login (y el MFA que dicte la política de Conditional Access del tenant — la aplicación no lo
   implementa), Entra ID llama de regreso a `/api/auth/callback/microsoft-entra-id`. NextAuth intercambia el
   código por tokens y los cifra dentro de la cookie de sesión `httpOnly` / `SameSite=Lax`
   (`callbacks.jwt` en `src/auth.ts` persiste `accessToken`, `refreshToken` y su expiración; refresca el
   access token automáticamente cuando expira, usando el token endpoint de Entra ID directamente).
4. El **access token nunca llega al JavaScript del navegador**: solo se lee server-side
   (`await auth()`) desde Server Components (`src/app/dashboard/page.tsx`) o Route Handlers. No hay ningún
   `SessionProvider`/`useSession()` en el cliente — es la barrera técnica real detrás del patrón BFF, no solo
   una intención de diseño.
5. `src/lib/api.ts#getMe` reenvía ese access token al backend como `Authorization: Bearer <token>` — la
   llamada real es servidor-a-servidor (Next.js → API), el navegador nunca ve el token.
6. El API valida el token con `Microsoft.Identity.Web` (`AddMicrosoftIdentityWebApi`, configurado desde la
   sección `EntraId` de `appsettings`/`user-secrets`): emisor, audiencia, firma contra las claves públicas
   reales del tenant (confirmado en logs: el API descarga las *signing keys* desde
   `https://login.microsoftonline.com/<tenant>/discovery/v2.0/keys`), expiración.
7. `CurrentUserProvisioningMiddleware` (`Infrastructure/Security/CurrentUserProvisioningMiddleware.cs`) corre
   justo después de la autenticación en cada request autenticado: lee los claims `oid`/`name`/`email` y
   ejecuta `ProvisionOrUpdateUserCommand`, que en el primer login crea el perfil local (`User.Provision`:
   `EntraObjectId` único, `IsActive = true`) y en cada login posterior solo actualiza el perfil y
   `LastLoginAtUtc`. Una cuenta desactivada recibe 403 en este punto, antes de llegar a cualquier controlador.
8. Cierre de sesión: `src/app/dashboard/page.tsx` expone un botón que ejecuta `signOut({ redirectTo: "/" })`
   — limpia la cookie de sesión local. (El *logout* federado contra el `end_session_endpoint` de Entra ID,
   para invalidar también la sesión del IdP, queda pendiente — no bloquea el uso normal.)
9. `middleware.ts` protege `/dashboard` (y cualquier ruta futura que se agregue a su `matcher`): sin sesión
   válida, redirige a `/` antes de renderizar nada.

## Verificación realizada

- **Backend**: `AssetManagement.IntegrationTests` (`AuthenticationAndRbacTests`) usa un `TestAuthHandler` que
  sustituye el esquema JwtBearer real — prueba aprovisionamiento, RBAC y empresa activa sin depender de
  credenciales de Entra ID.
- **Login real, de punta a punta, con un usuario del tenant**: el login interactivo (que Claude Code no puede
  ejecutar por sí mismo — requiere un humano, incluido el MFA que dicte la política del tenant) se completó
  con éxito. Quedó confirmado que:
  - El flujo CSRF + PKCE de NextAuth genera la URL de autorización correcta y Microsoft Entra ID la acepta.
  - Entra ID emitió un access token real, audience-scoped a la API (`api://<api-client-id>`).
  - El API validó ese token (firma, emisor, audiencia, expiración) contra las llaves reales del tenant.
  - `CurrentUserProvisioningMiddleware` creó el perfil local (`User`) con el `EntraObjectId`, nombre y correo
    reales del usuario en el primer login.
  - `GET /api/v1/me` respondió `200` con el perfil recién aprovisionado, mostrado en `/dashboard`.
  - `/dashboard` redirige correctamente a quien no tiene sesión.

  Dos ajustes de configuración de Entra ID (no de código) fueron necesarios para llegar a este resultado —
  quedaron documentados en `docs/security/entra-id-setup.md` ("Problemas ya encontrados y resueltos") para no
  repetirlos al configurar otro ambiente: el nombre exacto del scope (`access_as_user`, cuidado con typos) y
  la notificación opcional `email` en el **token de acceso** (por defecto Entra solo la incluye en el ID
  token, no en el access token que recibe la API).

## Autorización separada de autenticación

Entra ID únicamente prueba identidad (quién es el usuario). La estructura organizacional, roles y permisos se
administran **dentro de la aplicación** (ver `docs/security/authorization-rbac.md`) — Entra ID no se usa para
grupos/roles de la aplicación en V1.

## No implementado (explícitamente fuera de alcance)

- Cuentas locales o invitados sin Entra ID.
- Almacenamiento de contraseñas o hashes.
- Flujos de recuperación de contraseña.
- Administración de credenciales desde la aplicación.
- Logout federado (invalidar también la sesión del IdP al cerrar sesión).

## Cómo crear las credenciales

Paso a paso para dar de alta los App Registrations en el portal de Azure o vía Azure CLI:
[`docs/security/entra-id-setup.md`](entra-id-setup.md).
