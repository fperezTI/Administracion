# Reporte: Flujo de acceso, navegación y dashboard

## 1. Flujo de login completo

**Landing (`page.tsx`, Server Component):**
1. `auth()` (NextAuth v5) determina si hay sesión.
2. `getSystemInfo()` (`GET /api/v1/system/info`, anónimo) muestra estado del API: punto verde/rojo + `${producto} — ${entorno} — ${hora servidor es-MX}`. Si falla: `Home.systemInfoError`.
3. Sin sesión: formulario server action → `signIn("microsoft-entra-id", { redirectTo: "/dashboard" })`, botón `Home.signIn`.
4. Con sesión: botón `Home.goToDashboard`, simple Link, sin nuevo signIn.

**OAuth:**
5. Redirige a `login.microsoftonline.com/.../authorize`, scope `openid profile email offline_access api://{ENTRA_API_CLIENT_ID}/access_as_user` (Authorization Code + PKCE).
6. Usuario se autentica en Microsoft (incluye MFA si el tenant lo exige).
7. Callback `/api/auth/callback/microsoft-entra-id` (re-exporta handlers de `src/auth.ts`, sin lógica propia).
8. Callback `jwt` guarda accessToken/refreshToken/expiresAt cifrados; si expiró, `refreshAccessToken` hace POST directo al endpoint de token.
9. Callback `session` expone solo `session.accessToken` y `session.error`. **El access token nunca llega al navegador** (sin SessionProvider/useSession en ningún Client Component).
10. Redirección a `/dashboard`.

**En `/dashboard`:**
11. `requireAccessToken()` respaldo defensivo — si no hay token o hubo error de refresh, `redirect("/")`.
12. `getMe(accessToken)` → `GET /api/v1/me`.
13. Backend: `CurrentUserProvisioningMiddleware` — primer login crea perfil local (`User.Provision`, sin empresas/roles); logins posteriores actualizan DisplayName/Email/LastLoginAtUtc. Si cuenta desactivada: corta con 403 texto plano "La cuenta de usuario está desactivada." (fuera del GlobalExceptionHandler). Puebla `HttpContext.Items` con AccessibleCompanyIds y, si viene `X-Active-Company-Id` válido, ActiveCompanyId.
14. `MeController.Get()` sin permiso requerido — junta roles→permisos efectivos y empresas.
15. La página usa `me.permissionCodes.includes("Dashboards.ViewExecutive")`.

**Primer login vs. posterior:**
- Primer login: `PermissionCodes: []`, `Companies: []`. Dashboard queda **completamente en blanco** (a propósito, según comentario del código). Otras páginas muestran `EmptyCompanyState`: "Todavía no tienes acceso a ninguna empresa. Pide a un administrador que te agregue desde el módulo de usuarios."
- Login posterior: datos reales según asignaciones del admin.

## 2. Pantalla de login

Sin campos propios — 100% Entra ID. Textos (`es.json`, namespace Home):
```
title: "Gestión de Activos de TI e Infraestructura"
subtitle: "Registro, etiquetado y control de activos de TI e infraestructura en todas tus empresas."
systemInfoHeading: "Estado del API" (definida pero NO USADA en page.tsx)
systemInfoLoading: "Consultando el API..." (definida pero NO USADA)
systemInfoError: "No fue posible contactar al API."
signIn: "Iniciar sesión con Microsoft"
goToDashboard: "Ir a mi dashboard"
```
Selector de tema visible incluso sin sesión. Indicador de estado del API siempre visible.

## 3. Dashboard

Requiere sesión. Llama siempre a `GET /api/v1/me`. Controlado por permiso `Dashboards.ViewExecutive` (constante frontend `EXECUTIVE_DASHBOARD_PERMISSION`, backend valida independiente).

Si tiene el permiso, llama `GET /api/v1/dashboards/executive?companyId=...` y pinta 7 StatTile: Total de activos, Activos asignados, Activos disponibles, Aprobaciones pendientes (warning si >0), Mantenimientos abiertos, Garantías por vencer (warning si >0, ventana 30 días), Existencias bajas (destructive si >0).

**Hallazgo importante**: como el frontend nunca envía `X-Active-Company-Id`, `me.activeCompanyId` siempre es null → el dashboard ejecutivo **siempre muestra datos consolidados de todas las empresas del usuario**, sin forma de acotarlo a una sola empresa desde la UI (AppHeader del dashboard no recibe CompanySwitcher).

Errores: 403 → `Dashboard.accountDisabled` = "Tu cuenta está desactivada. Contacta a un administrador." Otro → `Dashboard.apiError` = "No fue posible consultar el dashboard en el API."

## 4. Navegación

**Barra superior (`app-header.tsx`)**: logo "AT" (solo `lg:hidden`), título/subtítulo, buscador (`GET /search?term=`), slot CompanySwitcher (si la página lo pasa), ThemeToggle, botón "Cerrar sesión" → `federatedSignOut`: `signOut({redirect:false})` + redirige a `end_session_endpoint` de Entra ID — **sí hay logout federado implementado**, pese a que `docs/security/authentication.md` dice que está pendiente (documentación desactualizada). Debajo del header siempre se renderiza `AppNav`.

**Menú principal**:
Autoservicio (visibles para todo usuario autenticado, sin filtrar por permiso): Mis asignaciones, Mis solicitudes, Mis aprobaciones, Mis notificaciones.

Grupos (acordeón, uno abierto a la vez):
| Grupo | Links |
|---|---|
| Activos | Activos, Categorías, Garantías |
| Inventario y movimientos | Asignaciones, Préstamos, Movimientos, Transferencias, Refacciones, Consumibles |
| Mantenimiento | Mantenimiento, Checklists |
| Solicitudes y aprobaciones | Solicitudes, Flujos de aprobación |
| Datos | Importaciones, Reportes, Plantillas |
| Administración | Empresas, Estructura, Roles, Usuarios, Auditoría |

**El menú no filtra por permisos del usuario** — todos ven todas las opciones; el control real es reactivo (403 de cada página).

**Selector de empresa activa**: sí existe (`CompanySwitcher`), contradiciendo parcialmente `docs/getting-started.md`. Funciona vía `?companyId=` en la URL, gestionado client-side — **no usa el header `X-Active-Company-Id`** en absoluto (confirmado por grep exhaustivo: el header solo aparece en middleware backend, tests y docs, nunca en `apps/web`). Usado en ~20 páginas de listado/alta. Ausente en `/dashboard` (siempre consolidado) y `/audit` (permiso tenant-wide sin membresía por empresa).

**Tema claro/oscuro**: `ThemeToggle`, toggle binario (no hay selector de 3 vías pese a que existen claves `light`/`dark`/`system` en `es.json` sin usar).

## 5. Navegación móvil / responsive

Breakpoint único: `lg` (1024px). <1024px: sidebar oculto, botón hamburguesa abre un Sheet (drawer) desde la izquierda, se cierra solo al navegar. >=1024px: sidebar fijo de 224px (`w-56`). El padding del body se activa vía selector CSS `:has([data-slot="app-sidebar"])`, sin que cada página lo sepa. Logo "AT" en header solo en `lg:hidden`.

Existe un Service Worker mínimo (`public/sw.js`, solo producción) que cachea el app-shell (network-first). No cachea datos de negocio. Posible inconsistencia: referencia `/manifest.webmanifest` pero el manifest real se genera dinámicamente en `manifest.ts` — no confirmado si coincide en producción.

## 6. Manejo de errores global

**No existe `error.tsx`, `global-error.tsx` ni `not-found.tsx`** en todo `apps/web/src/app`. Sin boundary de error global ni 404 personalizada.

- 401/expiración: `middleware.ts` + `requireAccessToken()` redirigen a `/` si no hay sesión o hubo error de refresh. Si un 401 llega desde una llamada al API con token supuestamente válido, no hay manejo específico — depende de si la página envuelve en try/catch.
- 403: cada página lo maneja ad hoc con texto hardcodeado específico del módulo. Sin componente compartido de "acceso denegado".
- 404: patrón repetido `if (status===404) notFound()`.
- 500/no mapeados: patrón `throw error` que cae en la página de error genérica de Next.js (sin personalizar). Backend centraliza en `GlobalExceptionHandler` (RFC 7807 ProblemDetails) pero la mayoría de páginas ignora el `detail` y muestra solo su texto fijo.

## 7. Mensajes de error/éxito relevantes

`es.json`: `Home.systemInfoError`, `Home.signIn`, `Home.goToDashboard`, `Dashboard.accountDisabled`, `Dashboard.apiError` (solo estos 5 relevantes a este flujo).

Hardcodeados: "La cuenta de usuario está desactivada." (backend, texto plano) / "Todavía no tienes acceso a ninguna empresa. Pide a un administrador que te agregue desde el módulo de usuarios." / mensajes 403 por página / "Cerrar sesión", "Buscar…" / "Empresa" (CompanySwitcher) / "Abrir menú", "Navegación".

## 8. Casos especiales

- Usuario sin empresa: `EmptyCompanyState` en páginas de listado; dashboard queda en blanco (sin el chequeo explícito, simplemente sin permiso).
- Usuario sin permisos (con empresa): 403 por página, menú no oculta nada.
- Expiración de sesión: refresh automático transparente; si el refresh_token expira, redirige a `/` sin mensaje explícito de "tu sesión expiró" (sin query param que lo distinga de una visita normal).
- Logout federado sí invalida sesión en Entra ID — discrepancia con `docs/security/authentication.md`.

### Discrepancias/ambigüedades documentación vs. código
1. Selector de empresa: docs dicen pendiente/basado en header; código tiene selector funcional pero paralelo y desconectado del header.
2. Dashboard ejecutivo no puede acotarse a una empresa desde la UI, sin indicador visual de "vista consolidada".
3. `es.json` solo cubre 3 pantallas de ~85 páginas totales.
4. Sin `error.tsx`/`global-error.tsx`/`not-found.tsx` en todo el árbol.
