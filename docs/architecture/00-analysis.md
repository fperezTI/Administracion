# Análisis inicial y plan de implementación — Sistema de Gestión de Activos de TI

Estado: **Entregable fundacional #1**. Fecha: 2026-09-15. Autor de la solicitud: echiveste@gmail.com.

Este documento es el registro persistente del análisis presentado como primera entrega. Los documentos
enfocados (`docs/security/*.md`, `docs/multi-company.md`, `docs/deployment.md`, etc.) son la referencia viva
que se mantendrá actualizada en incrementos posteriores; este archivo no se debe reeditar retroactivamente,
solo se referencia.

## 1. Evaluación del repositorio actual

- El directorio `C:\Staff\Administracion` estaba vacío, sin `.git`, sin código previo. Proyecto **greenfield**.
- Herramientas verificadas en la máquina de desarrollo:
  - .NET SDK: solo 8.0.425 estaba instalado. Se instaló **.NET SDK 9.0.318** vía `winget` (acción local,
    reversible, sin credenciales) para poder cumplir el requisito de ASP.NET Core 9. Ambos SDKs conviven;
    `global.json` fija el proyecto a 9.x.
  - Node.js v24.18.0, npm 11.16.0 — compatibles con Next.js 15.
  - Docker 29.7.2 — disponible para Docker Compose local.
  - Git 2.55 — disponible; el repositorio Git aún no se ha inicializado (se hace en este incremento).
- No hay convenciones, dependencias ni código reutilizable que heredar. Todas las convenciones se definen
  desde cero en `CLAUDE.md` y en `docs/`.

## 2. Resumen de requisitos entendidos

Aplicación web empresarial **multiempresa** (hasta 50 empresas, un único tenant de Microsoft Entra ID) para
gestión operativa integral de activos de TI: alta/recepción, movimientos, asignaciones/préstamos/devoluciones,
transferencias (incluyendo entre empresas), mantenimientos (preventivo/correctivo/garantía), solicitudes
internas con aprobación configurable, refacciones serializadas y consumibles por existencias, firma
electrónica simple, documentos/evidencias en Blob Storage, identificación física (QR por defecto, extensible a
código de barras/NFC/RFID), auditoría inmutable, reportes/paneles, búsqueda avanzada, notificaciones,
internacionalización preparada (UI en español V1), privacidad/retención configurable, RBAC global
(permisos no acotados por empresa), autenticación exclusiva vía Entra ID. **Fuera de alcance V1**: depreciación,
contabilidad de activos, integraciones financieras/ERP, alta disponibilidad formal, DR formal, Teams,
multi-tenant de Entra ID.

Pila obligatoria: Next.js 15 + React + TS estricto + Tailwind + shadcn/ui (PWA, temas, i18n-ready) /
ASP.NET Core 9 + C# nullable + Clean Architecture + DDD ligero + CQRS selectivo / EF Core + SQL Server /
Azure SQL / Azure Blob Storage / Serilog + OpenTelemetry + App Insights / xUnit + Playwright / Docker Compose /
Bicep + GitHub Actions + OIDC.

## 3. Contradicciones, riesgos y supuestos

### 3.1 Contradicciones y ambigüedades detectadas (con resolución propuesta)

| # | Tensión | Resolución adoptada |
|---|---|---|
| C1 | RBAC "global, no restringido por empresa" vs. modelo multiempresa con datos por `CompanyId` | Se separan dos ejes ortogonales: **autorización** (¿puede el usuario ejecutar la acción X?) evaluada por rol/permiso global, y **alcance de datos** (¿sobre qué empresas puede operar?) evaluado por la membresía usuario↔empresa + la empresa activa. Un usuario sin permiso nunca ve el botón/endpoint; un usuario con permiso pero sin membresía a la empresa no puede operar sobre datos de esa empresa. Ambos se validan en backend. |
| C2 | Categorías de activos "configurable" pero la V1 exige una lista fija inicial | Catálogo `AssetCategory` editable en BD, sembrado (seed) con las 9 categorías iniciales. Ningún `enum` de categoría en código. |
| C3 | 11 tipos de unidades organizacionales (empresa, unidad de negocio, dirección, gerencia, departamento, área, equipo, sucursal, ubicación, almacén, centro de datos, proyecto) | Modelo genérico **`OrgUnit`** con `OrgUnitType` (catálogo configurable) + jerarquía auto-referenciada (`ParentOrgUnitId`) + `path` materializado para consultas. Evita 11 tablas casi idénticas; `Company` es la raíz de cada árbol. `Warehouse` se modela como `OrgUnit` de tipo Almacén para reutilizar jerarquía, pero implementa una interfaz de dominio adicional para las reglas específicas de existencias. |
| C4 | Motor de aprobaciones "totalmente configurable" (secuencial/paralelo, N-de-M, sustitución, escalamiento…) para ~14 tipos de operación | Alcance completo es grande. Se entrega en **fases**: V1.1 cubre secuencial y paralelo con N-de-M, prohibición de autoaprobación y rechazo/cancelación; sustitución temporal y escalamiento automático se entregan en V1.2 (motor ya preparado por diseño, solo falta UI/job de escalamiento). Se documenta como decisión no bloqueante. |
| C5 | Importación de hasta 10,000 filas/lote — un request HTTP síncrono no es viable a esa escala con validación fila a fila | Se adopta **procesamiento asíncrono desacoplado**: el endpoint sube el archivo, encola un job (Azure Storage Queue en la nube; `Channel<T>` + `BackgroundService` en local/Docker Compose) y el usuario consulta el resultado por lote (`ImportBatch` con estado y reporte de errores por fila). Se elige Azure Storage Queue sobre Service Bus para V1 por simplicidad y por no requerir un recurso adicional no mencionado en la sección 3 del pedido; documentado como supuesto no bloqueante, revisable. |
| C6 | Firma electrónica simple registra IP/dispositivo/hash, pero la sección 28 exige proteger datos personales en logs | Estos datos son **auditoría de dominio** (tabla `SignatureRecord`), no logs técnicos. Los logs técnicos (Serilog) nunca deben contener PII; se aplican *destructuring policies*/scrubbing. |
| C7 | "No eliminar roles/permisos con historial" vs. edición de roles | Roles y permisos usan **baja lógica** (`IsActive`) nunca borrado físico si tienen asignaciones históricas; el sistema bloquea el borrado físico y solo permite desactivar. |
| C8 | CompanyId no debe modificarse directamente, solo vía transferencia completada, pero folios/secuencias deben ser por empresa | Se implementa `FolioSequence` (empresa + tipo de documento) con concurrencia optimista y reintento, no `IDENTITY` global ni `SEQUENCE` de SQL Server (que no son naturalmente particionables por clave). |
| C9 | "CQRS únicamente donde aporte valor" es subjetivo | Se fija criterio explícito (ver §4): CQRS con MediatR para módulos con reglas de negocio/validación no triviales (activos, movimientos, aprobaciones, mantenimientos, importaciones); *application services* simples para catálogos CRUD. |

### 3.2 Riesgos técnicos

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Volumen de referencia (250k activos, 2M movimientos, 500 usuarios concurrentes) sin diseño de índices/particionamiento desde el día uno | Degradación de performance en fases avanzadas | Índices definidos por patrón de consulta desde el modelo inicial (ver `docs/architecture/domain-model.md`), paginación obligatoria desde el primer endpoint, particionamiento de `Movement`/`AuditEntry` por fecha evaluado en fase de reportes. |
| Motor de aprobaciones + máquina de estados + firma + auditoría son todos *cross-cutting* | Riesgo de acoplar reglas de dominio a la capa API | Se modelan como *bounded contexts* propios con contratos de dominio (interfaces) consumidos por los módulos operativos vía Application layer, nunca invocados desde controladores. |
| Autenticación Entra ID sin tenant/app registrations reales todavía | Bloquea pruebas end-to-end de login | Se construye el pipeline de autenticación (JWT Bearer, validación de emisor/audiencia) parametrizado por configuración; se documenta como información externa pendiente (§18), no bloquea el resto del backlog. |
| PWA + escaneo QR requiere HTTPS y cámara | No funciona en `http://localhost` sin certificado | Docker Compose local expone HTTPS con certificado de desarrollo (`dotnet dev-certs`) para el API; Next.js dev server usa HTTP pero el escaneo se prueba contra el túnel/HTTPS de despliegue o con `next dev --experimental-https`. |
| Sobreingeniería del motor de flujos de aprobación / RBAC | Retrasa V1 | Backlog fasea explícitamente lo mínimo viable primero (ver §13). |

### 3.3 Supuestos no bloqueantes adoptados

1. Idioma del **código** (identificadores, nombres de tablas/clases): inglés, por convención de ingeniería; el
   idioma de la **interfaz y del contenido** (plantillas, catálogos, mensajes) es español y se resuelve vía
   recursos i18n. Esto no afecta la entrega en español de la UI.
2. Base de datos local de desarrollo: SQL Server 2022 en contenedor Docker; Azurite como emulador de Blob
   Storage en Docker Compose.
3. Autenticación del frontend hacia Entra ID: patrón *confidential client* con Route Handlers de Next.js
   actuando como intermediario ligero (BFF) que retiene el token en cookie `httpOnly` y lo reenvía como Bearer
   al API — reduce superficie de robo de token por XSS frente a guardarlo en `localStorage`/JS de cliente.
4. Formato de folio por defecto: `{PrefijoEmpresa}-{TipoDocumento}-{Consecutivo}` configurable por empresa
   (sección 8 del pedido: "prefijos y secuencias de folios").
5. Convención de commits: *Conventional Commits* (`feat:`, `fix:`, `chore:`…), documentado en `CLAUDE.md`.
6. Estrategia de traducciones: `next-intl` para frontend; recursos de plantillas versionados en BD (no
   archivos `.resx`) porque las plantillas requieren edición administrable en runtime.

## 4. Arquitectura propuesta

**Clean Architecture** en el backend con 4 capas físicas (proyectos .csproj) y **DDD ligero**: agregados con
invariantes protegidas, *value objects* para conceptos como `Money`, `SerialNumber`, `FolioNumber`; sin
generalización excesiva (no se usa Event Sourcing, no hay microservicios en V1 — monolito modular
desplegado como una sola Web API, con límites de módulo claros para permitir extracción futura si fuera
necesario).

- **Domain**: entidades, agregados, value objects, interfaces de repositorio, máquina de estados de activo,
  reglas de negocio puras, eventos de dominio. Sin dependencias externas.
- **Application**: casos de uso (comandos/consultas con MediatR donde aporta valor — ver criterio C9),
  validación (FluentValidation), interfaces de puertos (IDocumentStorage, IClock, ICurrentUser,
  ICurrentCompanyContext, INotificationSender), DTOs, *pipeline behaviors* (validación, autorización,
  auditoría, transacciones).
- **Infrastructure**: EF Core (DbContext, configuraciones, migraciones, repositorios), integración con Entra
  ID (validación de token), Azure Blob Storage, Serilog sinks, envío de correo, colas.
- **API**: controladores REST versionados (`/api/v1/...`), OpenAPI, middleware de manejo centralizado de
  errores (`ProblemDetails`), autenticación/autorización, *rate limiting*, CORS.

**Frontend**: Next.js 15 App Router, Route Handlers como BFF de autenticación, *server actions* para mutaciones
simples y *fetch* tipado hacia el API para el resto, componentes shadcn/ui, `next-intl`, tema claro/oscuro con
`next-themes`, PWA vía `next-pwa`/manifest + service worker propio (necesario para escaneo QR y "añadir a
inicio").

**Criterio CQRS (resuelve C9)**: se usa el patrón comando/consulta con MediatR en Assets, Movements,
Assignments, Approvals, Maintenance, Requests, Imports (módulos con reglas/transacciones no triviales).
Catálogos (marca, moneda, unidad de medida, etc.) usan *application services* CRUD simples sin
comandos/consultas individuales por operación, para no generar ceremonia innecesaria.

## 5. Contextos delimitados (bounded contexts)

| Contexto | Responsabilidad | Notas |
|---|---|---|
| **Identity & Access** | Usuarios, roles, permisos, membresía usuario-empresa | Perfil local creado en primer login vía Entra ID |
| **Organization** | Empresas, `OrgUnit` jerárquico, configuración por empresa | Ver C3 |
| **Asset Registry** | Activos, categorías, campos personalizados, identificación física/etiquetas | Agregado raíz `Asset` |
| **Inventory Operations** | Movimientos, asignaciones, préstamos, devoluciones, transferencias (incl. entre empresas) | Todo movimiento es inmutable una vez completado |
| **Maintenance** | Órdenes de mantenimiento, checklists versionados, MTTR/MTBF | Consume Spare Parts & Consumables |
| **Spare Parts & Consumables** | Refacciones serializadas, consumibles por existencia | Todo cambio de existencia genera un movimiento de inventario de consumible |
| **Requests** | Solicitudes internas y su ciclo de vida | Orquesta Approvals al aprobar dispara comandos en otros contextos |
| **Approvals** | Motor de flujos de aprobación configurable, genérico y reutilizado por otros contextos | Cross-cutting, sin conocimiento del dominio que aprueba |
| **E-Signature** | Captura y verificación de firma electrónica simple | Cross-cutting, reutilizado por Inventory Operations y Requests |
| **Documents** | Metadatos de documentos/evidencias, asociación polimórfica, integración con Blob Storage | Cross-cutting |
| **Templates** | Plantillas versionadas (resguardos, correos, notificaciones) | Cross-cutting |
| **Notifications** | Notificaciones in-app + correo, preferencias, reintentos | Cross-cutting, abstracción de canal (futuro Teams) |
| **Audit** | Auditoría funcional inmutable | Cross-cutting, independiente de logs técnicos |
| **Reporting** | Proyecciones/consultas de solo lectura para paneles y exportaciones | Consume los demás contextos vía consultas, nunca escribe |
| **Catalogs** | Catálogos configurables genéricos (marca, moneda, unidad de medida, proveedor, categoría) | Reutilizado transversalmente |
| **Import/Export** | Importación masiva, exportación de reportes | Ver C5 |

## 6. Entidades, agregados y relaciones principales

Detalle completo en `docs/architecture/domain-model.md`. Agregados raíz principales:

`Company`, `OrgUnit`, `User`, `Role`, `Permission`, `Asset`, `AssetCategory`, `AssetTag`, `Movement`,
`Assignment`, `Loan`, `Transfer`, `MaintenanceOrder`, `SparePart`, `Consumable`, `ConsumableStockMovement`,
`InternalRequest`, `ApprovalFlow` (configuración) + `ApprovalInstance` (ejecución), `SignatureRecord`,
`Document`, `Template` + `TemplateVersion`, `Notification`, `AuditEntry`, `ImportBatch`.

Relación clave: `Asset` **no** contiene `CompanyId` mutable directamente editable por API — solo se modifica
como efecto de una `Transfer` completada (regla de dominio, no solo de UI).

## 7. Estrategia RBAC

`User` → N `UserRole` → `Role` → N `RolePermission` → `Permission`. `Permission` tiene forma
`{Module}.{Action}` (p. ej. `Assets.Create`, `Approvals.Approve`, `Audit.Read`). Permisos **globales**,
nunca filtrados por empresa (resuelve C1). Verificación en tres capas:
1. UI: oculta/deshabilita controles (solo cosmético).
2. API: `[RequirePermission("Assets.Create")]` vía *policy-based authorization* + *pipeline behavior* de
   MediatR que re-valida antes de ejecutar el comando (defensa en profundidad — nunca confiar solo en el
   atributo del controlador).
3. Dominio: invariantes que no dependen de permisos pero sí de reglas (p. ej. "no autoaprobación") se validan
   en Application/Domain independientemente del RBAC.

Cambios de rol/permiso se auditan (Audit context). Roles/permisos con historial no se eliminan físicamente
(C7).

## 8. Estrategia multiempresa

Base de datos única, separación lógica por `CompanyId` en toda entidad transaccional, reforzada con
**EF Core Global Query Filters** (`HasQueryFilter(e => e.CompanyId == currentCompany)`) como defensa en
profundidad además de la validación explícita en cada *handler* — nunca se confía solo en el filtro implícito
ni solo en la empresa seleccionada en la UI (el backend deriva la empresa activa del contexto de sesión del
usuario autenticado + sus membresías, no de un parámetro de request no verificado). Detalle en
`docs/multi-company.md`.

## 9. Máquina de estados de activos

Detalle completo (incluyendo tabla de transiciones válidas, permisos y aprobaciones requeridas por
transición) en `docs/architecture/domain-model.md`. Estados: `InWarehouse, Reserved, Assigned, OnLoan,
InTransit, InMaintenance, UnderWarranty, Damaged, Lost, Stolen, PendingDecommission, Decommissioned, Sold,
Donated, Destroyed`. Implementada como reglas de dominio explícitas (no enum de terceros ni procedimiento
almacenado) en `AssetStateMachine`, configurable en el sentido de qué transiciones requieren evidencia,
aprobación o justificación (tabla `AssetStateTransitionRule`), pero el **grafo de transiciones válidas en sí
es código de dominio**, no datos — evita estados corruptos por configuración incorrecta.

## 10. Diseño del motor de aprobaciones

Contexto genérico y reutilizable (`Approvals`). Entidades: `ApprovalFlowDefinition` (qué tipo de operación,
roles autorizadores, número requerido, secuencial/paralelo, unanimidad/mínimo, ventana de vencimiento,
¿requiere comentario/evidencia?) y `ApprovalInstance` (ejecución concreta ligada a una operación de negocio
mediante referencia polimórfica `{ContextType, ContextId}`). El módulo que origina la aprobación (p.ej.
Requests, Inventory Operations) **no conoce** la lógica de aprobación: publica un comando
`RequestApproval(context, flowKey, payload)` y se suscribe al evento de dominio `ApprovalCompleted` /
`ApprovalRejected` para continuar su propio flujo. Reglas explícitas: prohibición de autoaprobación
(el solicitante nunca puede ser aprobador de su propia instancia), sustitución temporal y escalamiento
fasados a V1.2 (ver C4).

## 11. Estrategia de auditoría

Tabla `AuditEntry` de solo inserción (sin `UPDATE`/`DELETE` permitidos a nivel de permisos de base de datos
y sin endpoints de edición/borrado en el API). Se registra automáticamente vía *pipeline behavior* de
MediatR para todo comando marcado como sensible (interfaz `IAuditableCommand`), capturando: usuario, Object ID
de Entra ID, empresa activa/afectada, acción, módulo, entidad, id, valores antes/después (con *redacting* de
campos sensibles configurado por tipo), IP, agente, `CorrelationId` (propagado por middleware), motivo.
Separada físicamente de los logs técnicos de Serilog (que van a un *sink* distinto sin PII).

## 12. Estructura del monorepo

Ver estructura creada en este mismo incremento (§ "Entregable de este incremento" más abajo) y
`docs/architecture/overview.md`.

## 13. Backlog por fases

| Fase | Alcance | Depende de |
|---|---|---|
| **F0 — Fundacional** (este incremento) | Monorepo, solución backend (Domain/Application/Infrastructure/API vacíos pero compilando), Next.js base, Docker Compose, CI local (build+test), documentación base, `CLAUDE.md` | Nada externo |
| **F1 — Identity & Organization** | Autenticación Entra ID, perfil local, `Company`, `OrgUnit`, RBAC (roles/permisos/matriz), empresa activa | Requiere Tenant ID / App registrations (§18) para pruebas E2E reales; el código se construye contra configuración parametrizada mientras tanto |
| **F2 — Catálogos + Asset Registry** | Catálogos configurables, `AssetCategory` + campos personalizados, alta/recepción de activos, identificación QR (generación/reimpresión) | F1 |
| **F3 — Inventory Operations** | Movimientos, asignación/reasignación con firma, préstamo/devolución, transferencias intra-empresa | F2, motor de firma |
| **F4 — Approvals + E-Signature (núcleo)** | Motor de aprobación (secuencial/paralelo, N-de-M, anti-autoaprobación), firma electrónica simple multi-mecanismo, plantillas versionadas | F1 |
| **F5 — Transferencias entre empresas** | Flujo completo con aprobación + firma + cambio controlado de `CompanyId` | F3, F4 |
| **F6 — Mantenimientos + Refacciones/Consumibles** | Preventivo/correctivo/garantía, checklists, refacciones serializadas, consumibles por existencia | F3 |
| **F7 — Solicitudes internas** | Ciclo de vida completo, integración con F4 para generar asignaciones/movimientos automáticamente | F4, F3, F6 |
| **F8 — Documentos, Notificaciones, Auditoría UI** | Blob Storage, centro de notificaciones, panel de auditoría | Transversal, se apoya en todo lo anterior |
| **F9 — Importaciones/Exportaciones** | Importación masiva asíncrona, exportación Excel/PDF | F2+ |
| **F10 — Reportes y paneles** | Paneles operativos, KPIs (MTTR/MTBF, vencimientos, existencias bajas) | Todo lo anterior |
| **F11 — Búsqueda avanzada** | Búsqueda global multi-entidad con permisos | F2+ |
| **F12 — Privacidad/retención, hardening, carga** | Retención configurable, anonimización, pruebas de carga a escala de referencia | Todo lo anterior |
| **F13 — Azure/CI-CD productivo** | Bicep, GitHub Actions con OIDC, ambientes dev/test/prod | Requiere información de Azure (§18) |

## 14. Criterios de aceptación por fase

Cada fase se considera terminada solo bajo la "Definición de terminado" de la sección 39 del pedido original
(compila, pruebas pasan, conectado extremo a extremo, permisos validados en backend, auditado donde
corresponde, sin secretos, sin datos simulados en producción, migraciones incluidas y validadas,
documentación actualizada, errores manejados, accesibilidad revisada). Criterios específicos por fase se
detallarán al inicio de cada fase (no se anticipan aquí para evitar desalinearse del código real).

## 15. Estrategia de pruebas

Pirámide: xUnit para reglas de dominio (máquina de estados, motor de aprobación, cálculo de folios,
validaciones) con alta cobertura; pruebas de integración (Testcontainers para SQL Server + Azurite) para
persistencia/EF Core/Blob Storage; pruebas de contrato OpenAPI; Playwright para los flujos E2E críticos
listados en la sección 35 del pedido; `axe-core` embebido en Playwright para accesibilidad; pruebas de
seguridad (dependency/container scanning en CI, pruebas de autorización negativas —intento de autoaprobación,
acceso sin permiso—); k6 o similar para carga sobre los escenarios principales (fase F12).

## 16. Estrategia de migraciones

EF Core Migrations versionadas en `Infrastructure/Persistence/Migrations`, una migración por incremento
funcional (nunca una migración gigante). Aplicación controlada vía `dotnet ef database update` en local/CI de
pruebas; en Azure, aplicación como paso explícito y auditable del pipeline de despliegue (no
`EnsureCreated`, no migración automática en el arranque de producción). Toda migración se revisa por script
SQL generado (`dotnet ef migrations script`) antes de aplicarse a un ambiente compartido.

## 17. Riesgos técnicos y mitigaciones

Ver tabla en §3.2. Riesgo adicional de proceso: el pedido es extremadamente amplio; se mitiga entregando
**estrictamente por fases verificables**, sin avanzar a la fase N+1 sin que N compile y pruebe correctamente,
tal como exige la sección 2 del pedido.

## 18. Información externa realmente necesaria (no bloquea F0)

1. Microsoft Entra ID: **Tenant ID**, App registration del API (Application ID URI / scope expuesto),
   App registration del frontend (Client ID, tipo de flujo — se asume *Authorization Code + PKCE*
   confidencial vía Route Handlers), Redirect URIs por ambiente.
2. Suscripción de Azure de destino, convención de nombres de Resource Group, región primaria.
3. Nombre/SKU deseado de Azure SQL Database, Azure Blob Storage (Storage Account), Key Vault, Application
   Insights, Container Registry, App Service Plan (o decisión de usar Container Apps en su lugar — se asume
   App Service por instrucción explícita del pedido).
4. Organización/nombre del repositorio GitHub privado y si ya existe o se debe crear.
5. Dominios/URLs deseadas por ambiente (dev/test/prod) para configurar CORS y Redirect URIs.
6. Branding: nombre comercial del producto, logotipo por defecto, paleta de marca (para tema y plantillas).
7. Política real de retención de datos por defecto (mientras no se indique, se usa un valor conservador
   configurable, documentado como supuesto en `docs/privacy-retention.md` cuando se construya F12).

Ninguno de estos puntos bloquea el incremento fundacional (F0), que se construye a continuación.
