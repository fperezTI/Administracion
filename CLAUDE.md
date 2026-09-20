# CLAUDE.md

Convenciones y restricciones para trabajar en este repositorio. Léelo antes de tocar código.
Contexto completo del producto y del análisis inicial: `docs/architecture/00-analysis.md`.

## Qué es este repositorio

Monorepo de una aplicación empresarial multiempresa para gestión operativa de activos de TI e
infraestructura. Backend ASP.NET Core 9 (Clean Architecture) + frontend Next.js 15. V1 es
exclusivamente operativa (sin depreciación/contabilidad — ver `docs/roadmap.md`).

## Estructura

```
apps/api/src/AssetManagement.Domain          Reglas de dominio puras, sin dependencias
apps/api/src/AssetManagement.Application     Casos de uso (MediatR selectivo), puertos, validación
apps/api/src/AssetManagement.Infrastructure  EF Core, Entra ID, Blob Storage, adaptadores
apps/api/src/AssetManagement.Api             Controladores REST versionados, DI composition root
apps/api/tests/AssetManagement.UnitTests
apps/api/tests/AssetManagement.IntegrationTests
apps/web                                     Next.js 15 (App Router, TS estricto, Tailwind, shadcn/ui)
infrastructure/docker                        Dockerfiles
infrastructure/bicep                         IaC (Azure) — se puebla en F13
docs                                         Arquitectura, seguridad, operación, pruebas, roadmap
```

## Comandos

```bash
# Backend
dotnet build apps/api/AssetManagement.sln
dotnet test apps/api/tests/AssetManagement.UnitTests
dotnet test apps/api/tests/AssetManagement.IntegrationTests   # requiere Docker (Testcontainers)
dotnet ef migrations add <Nombre> --project apps/api/src/AssetManagement.Infrastructure --startup-project apps/api/src/AssetManagement.Infrastructure

# Frontend
cd apps/web && npm run dev | npm run build | npm run lint | npx tsc --noEmit

# Todo el stack
docker compose up -d --build
```

Ver `docs/getting-started.md` para el flujo completo.

## Reglas de arquitectura (no negociables)

1. **Dirección de dependencias**: `Domain` no depende de nada. `Application` depende solo de
   `Domain`. `Infrastructure` implementa los puertos de `Application`. `Api` es el composition
   root. Ninguna regla de negocio va en controladores, componentes de React ni procedimientos
   almacenados.
2. **CQRS selectivo**: MediatR solo en módulos con reglas/transacciones no triviales (Assets,
   Inventory Operations, Approvals, Maintenance, Requests, Import/Export). Catálogos simples usan
   *application services* CRUD directos. Ver ADR 0001.
3. **Multiempresa**: toda entidad transaccional lleva `CompanyId` explícito. Nunca se confía en un
   `CompanyId` recibido del cliente sin validar la membresía del usuario. `CompanyId` de un
   `Asset` nunca se edita directamente, solo vía `Transfer` completada. Ver `docs/multi-company.md`.
4. **RBAC**: permisos globales (`{Module}.{Action}`), nunca acotados por empresa. Se validan en
   backend (policy + `AuthorizationBehavior` de MediatR cuando exista), nunca solo en la UI. Ver
   `docs/security/authorization-rbac.md`.
5. **Auditoría**: `AuditEntry` es de solo inserción. Nunca agregues un endpoint de edición/borrado
   de auditoría. No mezcles auditoría funcional con logs técnicos de Serilog (los logs técnicos no
   llevan PII).
6. **Movimientos inmutables**: un `Movement`/`Transfer`/`Assignment` completado no se edita ni se
   borra. Las correcciones son movimientos compensatorios auditados.
7. **Máquina de estados de `Asset`**: el grafo de transiciones vive en código de dominio
   (`AssetStateMachine`), no en configuración ni en la base de datos. Ver
   `docs/architecture/domain-model.md`.

## Estilo de código

- C#: nullable reference types habilitado (ya forzado por `Directory.Build.props`), `var` cuando
  el tipo es evidente, namespaces de un archivo (`file_scoped`), sin comentarios que expliquen el
  "qué" (el código ya lo dice); comenta solo el "por qué" cuando no sea obvio.
- TypeScript: modo estricto (ya forzado por `tsconfig.json`), Server Components por defecto, usa
  `"use client"` solo cuando hay estado/efectos/eventos del navegador.
- Nombres de tablas/clases/identificadores en inglés; contenido de UI/plantillas en español vía
  `next-intl` (`apps/web/messages/es.json`). Ver supuesto en `docs/architecture/00-analysis.md`.
- Commits: Conventional Commits (`feat:`, `fix:`, `refactor:`, `docs:`, `chore:`, `test:`).

## Restricciones explícitas del cliente (no las rompas sin preguntar)

- Sin cuentas locales ni contraseñas — autenticación exclusiva vía Microsoft Entra ID
  (`docs/security/authentication.md`).
- Sin depreciación/contabilidad de activos en V1 (`docs/roadmap.md`).
- Sin secretos, credenciales ni identificadores reales de Azure/Entra ID en el repositorio — usa
  placeholders documentados y variables de entorno / Key Vault.
- No implementes "apariencia" de funcionalidad: cada botón/endpoint debe estar conectado de
  extremo a extremo o no debe existir todavía.
- No avances a la siguiente fase del backlog (`docs/architecture/00-analysis.md` §13) si la fase
  actual no compila y sus pruebas no pasan.

## Definición de terminado (resumen — detalle completo en el pedido original)

Compila · pruebas pasan · conectado extremo a extremo · permisos validados en backend · auditado
donde corresponde · sin secretos · sin datos simulados en producción · migraciones incluidas y
validadas · documentación actualizada · errores manejados consistentemente · accesibilidad
relevante revisada.
