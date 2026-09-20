# Puesta en marcha local

## Requisitos

- .NET SDK 9.x (`global.json` en la raíz fija la versión exacta)
- Node.js 24.x / npm 11.x
- Docker Desktop (Docker Compose v2)
- (Opcional pero recomendado) Credenciales de Entra ID — ver
  [`docs/security/entra-id-setup.md`](security/entra-id-setup.md). Sin ellas, el sistema funciona igual pero
  todo endpoint protegido responde `401` y el login del frontend no completa.

## Opción A — Todo en Docker Compose (recomendado para probar el sistema completo)

```bash
cp .env.example .env
# Edita .env: contraseña fuerte para SQLSERVER_SA_PASSWORD, y si ya tienes App Registrations
# (docs/security/entra-id-setup.md), también ENTRA_TENANT_ID, ENTRA_API_CLIENT_ID, ENTRA_WEB_CLIENT_ID,
# ENTRA_WEB_CLIENT_SECRET y AUTH_SECRET (genera este último con:
# node -e "console.log(require('crypto').randomBytes(32).toString('base64'))")
docker compose up -d --build
```

Servicios expuestos:

| Servicio | URL |
|---|---|
| Frontend (Next.js) | http://localhost:3000 |
| API (ASP.NET Core) | http://localhost:5080 |
| API — OpenAPI (solo Development) | http://localhost:5080/openapi/v1.json |
| API — health | http://localhost:5080/health/live, /health/ready |
| SQL Server | localhost,1433 |
| Azurite (Blob) | localhost:10000 |

Para detener: `docker compose down` (agrega `-v` solo si además quieres borrar los datos de SQL
Server/Azurite en los volúmenes con nombre).

## Opción B — Backend y frontend en modo desarrollo (hot reload)

Requiere SQL Server corriendo (puedes levantar solo ese servicio: `docker compose up -d sqlserver azurite`).

Backend:

```bash
cd apps/api
dotnet user-secrets set "ConnectionStrings:AssetManagementDb" "Server=localhost,1433;Database=AssetManagement;User Id=sa;Password=<la misma de tu .env>;TrustServerCertificate=True;" --project src/AssetManagement.Api
# Si ya tienes credenciales de Entra ID (docs/security/entra-id-setup.md):
dotnet user-secrets set "EntraId:Instance" "https://login.microsoftonline.com/" --project src/AssetManagement.Api
dotnet user-secrets set "EntraId:TenantId" "<tenant-id>" --project src/AssetManagement.Api
dotnet user-secrets set "EntraId:ClientId" "<api-client-id>" --project src/AssetManagement.Api
dotnet run --project src/AssetManagement.Api
```

Frontend:

```bash
cd apps/web
cp .env.example .env.local
# Completa ENTRA_TENANT_ID / ENTRA_CLIENT_ID / ENTRA_CLIENT_SECRET / ENTRA_API_CLIENT_ID / AUTH_SECRET
# si ya tienes credenciales (docs/security/entra-id-setup.md); sin ellas el login no completa, el resto
# de la app funciona igual.
npm install
npm run dev
```

## Migraciones de base de datos

```bash
dotnet tool restore  # o: dotnet tool install --global dotnet-ef
dotnet ef database update \
  --project apps/api/src/AssetManagement.Infrastructure \
  --startup-project apps/api/src/AssetManagement.Infrastructure
```

Nunca se aplican migraciones automáticamente al iniciar la API (ver `docs/operations.md`).

## Pruebas

```bash
# Backend — unitarias
dotnet test apps/api/tests/AssetManagement.UnitTests

# Backend — integración (requiere Docker; usa Testcontainers para levantar SQL Server real)
dotnet test apps/api/tests/AssetManagement.IntegrationTests

# Frontend
cd apps/web && npm run lint && npx tsc --noEmit && npm run build
```

## Verificación rápida de que todo está conectado

1. `docker compose up -d --build`
2. Abre http://localhost:3000 — debe mostrar la pantalla de inicio con el estado del API en vivo y un botón
   "Iniciar sesión con Microsoft".
3. `curl http://localhost:5080/health/live` → `Healthy`.
4. `curl -i http://localhost:5080/api/v1/companies` → `401` sin sesión (correcto — es RBAC funcionando, no un
   error). `curl http://localhost:5080/api/v1/system/info` sigue respondiendo `200` porque ese endpoint es
   intencionalmente anónimo.

## Autenticación en desarrollo local

Con credenciales de Entra ID configuradas (`docs/security/entra-id-setup.md`), el botón "Iniciar sesión con
Microsoft" de http://localhost:3000 dispara el flujo real (Authorization Code + PKCE) y termina en la página
de login genuina de Microsoft. Tras iniciar sesión, `/dashboard` muestra el perfil, permisos y empresas del
usuario, obtenidos de `GET /api/v1/me` con el token real — ver `docs/security/authentication.md` para el
detalle completo y lo que ya quedó verificado contra un tenant real.

Sin credenciales, todo endpoint protegido responde `401` — es el comportamiento correcto, no un error. Para
probar el flujo completo de autenticación, aprovisionamiento y RBAC sin depender de un tenant real, usa las
pruebas de integración del backend (`dotnet test apps/api/tests/AssetManagement.IntegrationTests`), que
sustituyen el validador de tokens por un `TestAuthHandler`.

### Primer acceso: cómo obtener acceso a una empresa

El primer login de cada usuario solo crea su perfil local — sin empresa ni permisos asignados, `/assets`
mostrará "no tienes acceso a ninguna empresa" y `/asset-categories` un `403`. Como todavía no existe la UI
de administración (roles, permisos, empresas — ver `docs/roadmap.md`), un administrador debe darlos de alta
manualmente por ahora: crear una `Company` (`Domain.Organization.Company.Create`), un `Role` con los
permisos necesarios (`Role.Create` + `Role.SetPermissions`, ver `docs/security/authorization-rbac.md` para
los códigos de permiso) y usar `User.GrantCompanyAccess` / `User.AssignRole` para conectarlos con el usuario
— vía una migración de datos, un script de EF Core, o `sqlcmd` directo contra el contenedor `sqlserver`
mientras no exista la UI.

La empresa activa se selecciona con el encabezado `X-Active-Company-Id: <guid>` en cada request al API — se
valida contra las empresas a las que el usuario autenticado realmente pertenece (`docs/multi-company.md`); el
frontend aún no expone un selector de empresa en la UI (pendiente, junto con el resto de la administración).
