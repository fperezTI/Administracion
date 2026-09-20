# Gestión de Activos de TI e Infraestructura

Aplicación empresarial multiempresa para la administración operativa integral de activos de TI:
altas, movimientos, asignaciones, préstamos, transferencias, mantenimientos, refacciones,
consumibles, solicitudes internas, flujos de aprobación configurables, firma electrónica simple,
documentos/evidencias, identificación por QR y auditoría inmutable.

**Estado**: **roadmap de V1 completo.** F0 (fundacional), F1 (Identity & Organization), F2 (Catálogos +
Asset Registry), F3 (Inventory Operations), F4 (Approvals + E-Signature — núcleo), F5 (Transferencias
entre empresas), F6 (Mantenimientos + Refacciones/Consumibles), F7 (Solicitudes internas), F8
(Documentos/Notificaciones/Auditoría UI), F9 (Importaciones/Exportaciones), F10 (Reportes y paneles), F11
(Búsqueda avanzada) y F12 (Privacidad/retención, hardening, carga) completos y probados de punta a punta,
**incluyendo login real** contra Microsoft Entra ID (patrón BFF: el
token nunca toca el navegador) con logout federado. Backend y frontend completos, no solo API: alta,
edición y etiquetado (QR) de activos con categorías configurables y campos personalizados; asignación de
activos a personas con **firma electrónica simple de autoservicio** (la persona confirma su propia
recepción, escribiendo su nombre o dibujando su firma), préstamos de corto plazo, devoluciones y
reubicaciones auditadas con historial de movimientos; **motor de aprobaciones genérico**
(secuencial/paralelo, N-de-M, prohibición de autoaprobación) conectado a la baja y disposición de activos,
a las **transferencias entre empresas** (aprobación + salida + tránsito + recepción con firma, con cambio
controlado de `CompanyId` y regeneración de folio en la empresa destino) y a las **solicitudes internas de
autoservicio** (un empleado pide que le asignen un activo, un préstamo, o reporta que necesita
mantenimiento; al aprobarse se genera automáticamente la asignación/préstamo/orden de mantenimiento
correspondiente), con roles aprobadores dinámicos configurables desde la UI y **centro de notificaciones**
(quien debe decidir una aprobación y quien la solicitó se enteran automáticamente); **mantenimientos**
preventivos/correctivos con checklists versionados, **garantías** por activo, **refacciones serializadas**
(con historial de instalación/retiro) y **consumibles por existencia** (cuya cantidad solo cambia mediante
movimientos auditados); **documentos/evidencias** en Azure Blob Storage adjuntos a activos y órdenes de
mantenimiento; **panel de auditoría** inmutable de acciones sensibles (quién, qué, cuándo, éxito o
fracaso); **importación masiva de activos** por archivo CSV con vista previa, validación fila por fila y
confirmación en modo "todo o nada" o "solo filas válidas", procesada de forma asíncrona en segundo plano; y
**exportación de activos a Excel/PDF**; **paneles operativos de reportes** (inventario por estado/categoría,
MTTR/MTBF de mantenimiento, garantías por vencer, existencias bajas) por empresa o consolidados entre
todas, con exportación a Excel/PDF; **búsqueda global** (activos, movimientos, mantenimiento, garantías,
refacciones, consumibles, solicitudes internas y usuarios, cada tipo visible solo si el usuario tiene el
permiso correspondiente) accesible desde cualquier pantalla; **anonimización de usuarios** y purga
automática de notificaciones por antigüedad (retención configurable), *rate limiting* y encabezados de
seguridad en API y frontend, y una suite de pruebas de carga real contra SQL Server (ver
`docs/performance.md`); administración de categorías, empresas, estructura organizacional jerárquica y
**UI completa de RBAC** (roles, matriz de permisos, asignación de roles y accesos por empresa a usuarios).

**F13 (Azure/CI-CD productivo)** entrega la infraestructura como código (`infrastructure/bicep/`) y los
flujos de CI/CD (`.github/workflows/`) completos y validados de sintaxis — pero sin un despliegue real:
esta conversación nunca tuvo una suscripción de Azure ni un repositorio de GitHub remoto conectados. Ver
`docs/deployment.md` para la guía paso a paso de despliegue con información real.

Ver `docs/architecture/00-analysis.md` para el análisis completo y `docs/roadmap.md` (incluye el detalle
por fase, con lo pendiente de cada una) para el backlog.

## Empezar

```bash
cp .env.example .env    # define SQLSERVER_SA_PASSWORD
docker compose up -d --build
```

Frontend: http://localhost:3000 · API: http://localhost:5080 ·
Guía completa: [`docs/getting-started.md`](docs/getting-started.md).

## Documentación

| Tema | Documento |
|---|---|
| Análisis inicial completo (requisitos, riesgos, backlog) | [`docs/architecture/00-analysis.md`](docs/architecture/00-analysis.md) |
| Arquitectura | [`docs/architecture/overview.md`](docs/architecture/overview.md) |
| Modelo de dominio | [`docs/architecture/domain-model.md`](docs/architecture/domain-model.md) |
| Decisiones arquitectónicas (ADRs) | [`docs/architecture/decisions/`](docs/architecture/decisions/) |
| Autenticación | [`docs/security/authentication.md`](docs/security/authentication.md) |
| Crear los App Registrations de Entra ID | [`docs/security/entra-id-setup.md`](docs/security/entra-id-setup.md) |
| Autorización / RBAC | [`docs/security/authorization-rbac.md`](docs/security/authorization-rbac.md) |
| Hardening (rate limiting, encabezados de seguridad) | [`docs/security/hardening.md`](docs/security/hardening.md) |
| Privacidad y retención de datos | [`docs/privacy-retention.md`](docs/privacy-retention.md) |
| Pruebas de carga | [`docs/performance.md`](docs/performance.md) |
| Modelo multiempresa | [`docs/multi-company.md`](docs/multi-company.md) |
| Despliegue | [`docs/deployment.md`](docs/deployment.md) |
| Operación | [`docs/operations.md`](docs/operations.md) |
| Pruebas | [`docs/testing.md`](docs/testing.md) |
| Roadmap | [`docs/roadmap.md`](docs/roadmap.md) |
| Convenciones para trabajar en el repo | [`CLAUDE.md`](CLAUDE.md) |

## Pila tecnológica

Next.js 15 + TypeScript estricto + Tailwind + shadcn/ui (frontend) · ASP.NET Core 9 + Clean
Architecture + DDD ligero + CQRS selectivo (backend) · EF Core + SQL Server/Azure SQL ·
Azure Blob Storage · Serilog + OpenTelemetry · Microsoft Entra ID (única identidad) · xUnit +
Playwright · Docker Compose · Bicep + GitHub Actions con OIDC (infraestructura y CI/CD listos desde F13 —
sin despliegue real ejecutado, ver `docs/deployment.md`).

## Licencia

Privado — uso interno.
