# Arquitectura — visión general

> Documento vivo. El razonamiento y las alternativas consideradas están en
> `docs/architecture/00-analysis.md` y en `docs/architecture/decisions/` (ADRs). Este archivo describe el
> estado actual del sistema.

## Estilo

Monolito modular con **Clean Architecture** (4 capas: Domain, Application, Infrastructure, API) y **DDD
ligero**. Sin microservicios en V1; los límites de contexto (ver abajo) están diseñados para permitir
extracción futura si el crecimiento lo justificara, sin que eso implique código o abstracciones sin uso hoy.

## Diagrama de capas (backend)

```
apps/api/src/
  Company.Assets.Domain/            Entidades, agregados, value objects, reglas de negocio puras
  Company.Assets.Application/       Casos de uso (MediatR selectivo), validación, puertos (interfaces)
  Company.Assets.Infrastructure/    EF Core, Entra ID, Blob Storage, colas, email, Serilog
  Company.Assets.Api/               Controladores REST versionados, OpenAPI, middleware, DI composition root
```

Regla de dependencia: `Domain` no depende de nada. `Application` depende solo de `Domain`. `Infrastructure`
implementa los puertos definidos en `Application`. `Api` depende de `Application` e `Infrastructure` (solo
para *composition root*/DI, nunca lógica de negocio en controladores).

## Frontend

Next.js 15 (App Router) en `apps/web`, TypeScript estricto, Tailwind + shadcn/ui, PWA instalable, tema
claro/oscuro, i18n preparado con `next-intl` (español como único locale activo en V1). Route Handlers actúan
como intermediario de autenticación (ver `docs/security/authentication.md`) reenviando tokens al API.

## Contextos delimitados

Ver `docs/architecture/00-analysis.md` §5 para la tabla completa. Resumen: Identity & Access, Organization,
Asset Registry, Inventory Operations, Maintenance, Spare Parts & Consumables, Requests, Approvals,
E-Signature, Documents, Templates, Notifications, Audit, Reporting, Catalogs, Import/Export.

## Comunicación entre contextos

Dentro del mismo proceso: comandos/eventos de dominio vía MediatR (`INotificationHandler` para eventos de
dominio publicados tras `SaveChanges`). Ningún contexto referencia directamente las entidades internas de
otro; se comunican por interfaces de Application (puertos) y eventos, incluso estando en el mismo monolito —
esto es lo que permite, a futuro, extraer un contexto a un servicio propio sin reescribir el dominio.

## Persistencia

Un único `AppDbContext` de EF Core para V1 (esquema compartido, separación lógica por `CompanyId`), con
`DbSet` agrupados por *configuration classes* (`IEntityTypeConfiguration<T>`) organizadas por contexto en
`Infrastructure/Persistence/Configurations/{Context}/`. Ver `docs/multi-company.md` para el detalle de
aislamiento lógico.

## Observabilidad, errores, seguridad

Ver `docs/operations.md` y `docs/security/*.md`.
