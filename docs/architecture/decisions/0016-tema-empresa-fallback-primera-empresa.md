# ADR 0016 — Herencia de tema: fallback a la primera empresa cuando no hay empresa activa explícita

## Estado
Aceptado (2026-09-27).

## Contexto
El pedido original de theming especifica el orden de resolución: preferencia personal → tema por defecto
de la **empresa activa** → `light`. `docs/multi-company.md` ya documentaba el mecanismo para "empresa
activa" (`ICurrentCompanyContext`, poblado por `CurrentUserProvisioningMiddleware` a partir del
encabezado `X-Active-Company-Id`), pero también advertía explícitamente: *"los módulos que operan
explícitamente 'sobre la empresa activa' (aún no hay ninguno en F1) la leerán de aquí cuando se
construyan"*.

Al implementar el sistema de temas se confirmó que ningún llamador del frontend (`apps/web/src/lib/api.ts`
→ `apiFetch`) envía ese encabezado todavía — no existe ningún selector de "empresa activa" persistente a
nivel de sesión en la aplicación; cada página de negocio recibe `companyId` como parámetro explícito de
URL/query, validado contra la membresía real en cada handler (cumpliendo la regla de multiempresa), sin
relación con `ICurrentCompanyContext`. En la práctica, `ActiveCompanyId` es `null` en toda request actual.

Con la implementación original de `GetMeQueryHandler`, esto significaba que **"usar tema de la empresa"
nunca heredaba nada**: sin importar cuál fuera el `DefaultThemeCode` de la empresa, todo usuario sin
preferencia personal explícita veía siempre el fallback `light`, incluso teniendo una única empresa
inequívoca.

## Decisión
`GetMeQueryHandler` usa como *fallback* la primera empresa accesible del usuario (por orden de alta, vía
`UserCompanies`) cuando no hay una empresa activa explícita:

```csharp
var activeCompany = companies.FirstOrDefault(c => c.CompanyId == currentCompany.CompanyId)
    ?? companies.FirstOrDefault();
```

No se construyó un mecanismo de selección de "empresa activa" persistente a nivel de sesión (cookie,
claim, o similar) para resolver esto de forma más precisa, porque:

- No fue pedido explícitamente, y añadirlo habría sido una pieza de infraestructura nueva y no trivial
  (persistencia, invalidación al cambiar de empresa, interacción con cada página que ya maneja su propio
  `companyId` por query param) para un problema que, en la base de usuarios real actual, no existe: hay
  una sola empresa dada de alta.
- El fallback es inocuo desde el punto de vista de seguridad — solo afecta qué **tema visual** se
  muestra, nunca qué datos son accesibles (la validación de membresía real sobre `CompanyId` en cada
  operación de negocio es completamente independiente de este mecanismo, ver `docs/multi-company.md`).
- Es coherente con el propio texto de `docs/multi-company.md`, que ya anticipaba que el primer módulo en
  "leer de aquí" definiría cómo se comporta en ausencia del encabezado.

## Consecuencias
- Con una sola empresa (caso real actual), "usar tema de la empresa" funciona sin ambigüedad.
- Con varias empresas y sin `X-Active-Company-Id` ni un selector de empresa activa persistente, todo
  usuario sin preferencia personal hereda el tema de su **primera** empresa por orden de alta — no
  necesariamente la que esté "viendo" en una página con otro `companyId` en la URL. Esto es una
  limitación conocida y documentada, no un bug: cuando se construya un selector de empresa activa
  persistente (que enviaría `X-Active-Company-Id` en cada request), este fallback deja de ejercitarse de
  forma natural sin requerir otro cambio en `GetMeQueryHandler`.
- `GetMeQueryHandlerTests.Inherits_the_first_companys_theme_when_no_active_company_is_explicitly_selected`
  fija este comportamiento explícitamente.
