# ADR 0010 — Documentos/Notificaciones/Auditoría: alcance V1 y el descubrimiento de `ActiveCompanyId`

## Estado
Aceptado (F8, 2026-09-17).

## Contexto
F8 agrupa tres subsistemas transversales que el pedido anticipó desde F0 (`INotificationSender` en la
arquitectura, `IAuditableCommand` en la estrategia de auditoría, Azurite en `docker-compose.yml`) pero que
nadie había construido todavía. Cada uno tenía una ambigüedad real que resolver, y al construir Auditoría
apareció además un descubrimiento que cambió el diseño original.

## Decisiones

### 1. Documentos
Puerto `IFileStorage` sobre Azure.Storage.Blobs, contra Azurite en desarrollo y una cuenta real en
producción — misma estrategia de cadena de conexión por ambiente que SQL Server. `Document` es una
asociación polimórfica (`EntityType`/`EntityId`) validada contra un allowlist estático
(`DocumentEntityTypes`, V1: `Asset` y `MaintenanceOrder` — los dos puntos donde el pedido ya habla de
"evidencias"), mismo patrón que `SignatureRecord.ContextType/ContextId` desde F3. Sin edición ni borrado
(coherente con que el catálogo de permisos solo sembró `Documents.Read`/`Documents.Create`, nunca
Update/Delete). La descarga se sirve **streameada por la propia API** (revalida permiso + empresa en cada
descarga) en vez de URLs SAS con expiración.

### 2. Notificaciones
`Notification` es una fila simple por usuario, sin subsistema de reintentos con cola en segundo plano — el
pedido pide "reintentos" pero no hay infraestructura de colas en el proyecto ni un proveedor SMTP
configurado. `INotificationSender` (Application) siempre escribe la fila in-app y además intenta el correo
**una sola vez, mejor esfuerzo**, vía `IEmailSender` (Infrastructure): SMTP real si `Smtp:Host` está
configurado, si no, un sender que solo registra en Serilog — nunca finge enviar un correo que no envió.
Disparadores de V1: dos *reaction handlers* genéricos sobre eventos de Approvals (`ApprovalRequested`,
agregado a `ApprovalInstance.Create` en esta fase, y los ya existentes `ApprovalCompleted`/
`ApprovalRejected`) que no filtran por `ContextType` — cubren automáticamente decommission/disposal (F4),
transferencias (F5) y solicitudes internas (F7) sin tocar esos módulos.

### 3. Auditoría — y el descubrimiento de `ActiveCompanyId`
`IAuditableCommand` es una interfaz marcador vacía (mismo patrón que `IRequiresPermission`), detectada por
un nuevo `AuditBehavior` (pipeline de MediatR, después de `ValidationBehavior`). Se marcó en un barrido
representativo de ~25 comandos sensibles ya existentes en cada módulo — no los ~60 comandos totales del
sistema; agregar la marca a más comandos es una línea adicional, no bloqueante. Sin diff de "valores
antes/después" con redacción por campo — se sustituye por una traza real: comando, parámetros tal como se
enviaron (JSON), resultado, IP, user-agent, `CorrelationId` (nuevo en `ICurrentUserContext`, resuelto de
`HttpContext.TraceIdentifier` — el mismo id que ya llevaría la línea de Serilog de esa petición).

**Descubrimiento real durante la construcción**: el diseño original de `AuditEntry.CompanyId` lo tomaba de
`ICurrentCompanyContext.CompanyId` (la "empresa activa"). Al escribir las pruebas de integración se
descubrió que **el frontend nunca envía el encabezado `X-Active-Company-Id`** — cada página de este
proyecto, desde F1, pasa `companyId` como parámetro explícito en cada consulta/comando en vez de usar el
mecanismo implícito que `multi-company.md` documentó desde el principio ("aún no hay ninguno [módulo] en
F1... la leerán de aquí cuando se construyan" — y ninguno, hasta F8, lo hizo). Esto significa que
`ICurrentCompanyContext.CompanyId` es `null` en el 100% de las peticiones reales de este sistema, no solo
en las pruebas — diseñar `AuditEntry.CompanyId` a partir de ese campo lo habría dejado permanentemente
vacío, y además el filtro de consulta que se planeó originalmente (visible solo para quien tuviera acceso
a esa empresa) habría sido un colador: toda fila con `CompanyId == null` habría sido visible para
cualquiera con `Audit.Read` sin importar a qué empresas pertenece — filtrando por una empresa que nunca se
llena no protege nada.

Se resolvió así, una vez identificado el problema real: **`AuditEntry` no lleva filtro de consulta por
empresa en absoluto** — mismo tratamiento que `Company`/`Role` (sin filtro de membresía tampoco):
`Audit.Read` es un permiso administrativo global, del mismo tipo que `Companies.Read`/`Roles.Read`, no uno
acotado por empresa. `AuditEntry.CompanyId` se sigue registrando cuando es posible — `AuditBehavior` lo
obtiene por reflexión de una propiedad pública `CompanyId` en el propio comando cuando existe (la mayoría
de los `Create`/registro sí la tienen; comandos que solo referencian `AssetId`/`RoleId`/etc. simplemente
quedan sin ese dato) — pero es un filtro de **presentación**, no de seguridad; `GetAuditEntriesQuery.CompanyId`
pasó de obligatorio a opcional.

## Consecuencias
- `docs/multi-company.md` debería actualizarse en una fase futura para dejar de anticipar que algún módulo
  usará `X-Active-Company-Id`/`ActiveCompanyId` — después de ocho fases, ninguno lo hizo, y F8 confirma por
  qué: cada pantalla ya sabe con qué empresa está trabajando por su propio contexto (el activo que se está
  viendo, la empresa elegida en el `CompanySwitcher`, etc.), así que un mecanismo implícito adicional nunca
  hizo falta. No se cambia en esta fase para no desviarse del alcance de F8.
- `/audit` en el frontend no lleva `CompanySwitcher` — mismo criterio que `/companies`.
- Verificado por integración: un comando exitoso queda auditado con sus detalles y `CompanyId` cuando el
  comando lo expone; uno fallido queda auditado con el mensaje de error y `CompanyId` nulo cuando no lo
  expone, visible igual para cualquiera con `Audit.Read`.
