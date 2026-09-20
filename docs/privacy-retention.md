# Privacidad y retención de datos

Referenciado desde `docs/architecture/00-analysis.md` §18 desde F0 ("política real de retención de datos
por defecto... documentado como supuesto en `docs/privacy-retention.md` cuando se construya F12") — este
documento se construyó en F12, ver ADR 0014.

## Alcance y base legal

El pedido no indicó una política de retención real, así que este documento fija **valores conservadores y
configurables**, no una política legal definitiva — un supuesto documentado, revisable en cualquier
momento cambiando la configuración correspondiente (nunca requiere tocar código). Dado que toda fecha/hora
de negocio de este sistema usa `America/Mexico_City` (ver `docs/multi-company.md`), la referencia legal
natural es la Ley Federal de Protección de Datos Personales en Posesión de los Particulares (LFPDPPP) de
México — en particular el principio de **calidad** (los datos personales deben conservarse solo el tiempo
necesario) y el derecho de **cancelación** (equivalente al "derecho al olvido"). Este documento no
sustituye asesoría legal; documenta la implementación técnica que la soporta.

## Inventario de datos personales (PII) por entidad

| Entidad | Campo(s) | Naturaleza |
|---|---|---|
| `User` | `DisplayName`, `Email` | Identidad del usuario autenticado (viene de Microsoft Entra ID) |
| `SignatureRecord` | `IpAddress`, `UserAgent`, nombre escrito en `TypedConfirmation` | Metadatos de firma electrónica simple (pedido C6) |
| `AuditEntry` | `IpAddress`, `UserAgent`, `UserDisplayName` (denormalizado) | Rastro de auditoría funcional |
| `Notification` | `Body` (puede mencionar nombres/folios) | Contenido de notificaciones in-app |

`Movement`/`Assignment`/`Transfer`/`InternalRequest` no llevan PII propia más allá de referencias por
`Guid` a `User` — no se anonimizan directamente, su vínculo con un usuario ya anonimizado simplemente deja
de resolver a un nombre real (ver más abajo).

## Qué se retiene indefinidamente en V1 (y por qué)

`Movement`, `Transfer`, `Assignment`, `SignatureRecord` y `AuditEntry` son el **registro legal de
custodia y de acciones sensibles** del sistema (pedido: auditoría inmutable, trazabilidad de activos) —
no se purgan automáticamente en V1. Purgarlos ciegamente después de N días rompería la trazabilidad que
la propia auditoría existe para garantizar. Si en el futuro se requiere una política de purga real sobre
estas tablas (p. ej. por antigüedad extrema), debe ser una decisión de negocio explícita, no un valor por
defecto — documentado aquí como punto de extensión, no implementado.

## Qué se purga automáticamente en V1

**`Notification`** — ya documentada desde F8 como "efímera por diseño" (una vez leída/vista, no aporta
valor de auditoría; el evento de negocio que la originó ya quedó en `AuditEntry`/`Movement` si aplica). Un
`BackgroundService` (`NotificationRetentionBackgroundService`, ver ADR 0014) borra las notificaciones con
más de `DataRetention:NotificationRetentionDays` días (**90 por defecto**, configurable) cada
`DataRetention:PurgeIntervalHours` horas (**24 por defecto**).

## Anonimización (derecho de cancelación / "al olvido")

Un administrador con `Users.Update` puede anonimizar un perfil de usuario desde `/users/{id}` — acción
**irreversible**: `DisplayName`/`Email` se reemplazan por valores sintéticos, la cuenta se desactiva.

- **No borra registros históricos** — `AuditEntry.UserDisplayName` (denormalizado desde F8, ADR 0010) y
  cualquier `Movement`/`Assignment`/`SignatureRecord` que referencie al usuario por `Guid` conservan lo que
  ya se escribió. Anonimizar el perfil detiene la exposición futura del nombre/correo reales; no reescribe
  el pasado — el mismo criterio que ya rige `AuditEntry` desde F8 (es un registro de solo inserción).
- **No borra el usuario** — la fila `User` permanece (con `Id` intacto) para no romper ninguna clave
  foránea del sistema; solo su contenido identificable se reemplaza.
- Un usuario ya anonimizado no puede volver a anonimizarse (operación idempotente-negativa, no silenciosa
  — el intento repetido devuelve un error explícito).

## Configuración

| Clave | Por defecto | Efecto |
|---|---|---|
| `DataRetention:NotificationRetentionDays` | `90` | Antigüedad máxima de una `Notification` antes de purgarse |
| `DataRetention:PurgeIntervalHours` | `24` | Frecuencia del ciclo de purga |

## Pendiente (no bloqueante, fuera de alcance de V1)

- Exportación de datos personales a solicitud del titular ("derecho de acceso/portabilidad") — no pedido
  explícitamente, se documenta como extensión futura.
- Retención/purga configurable sobre `Movement`/`AuditEntry` más allá de lo indefinido — requiere una
  decisión de negocio real, no un valor por defecto asumido.
