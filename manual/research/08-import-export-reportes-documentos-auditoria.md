# Reporte: módulos transversales (Import/Export, Reportes, Documentos, Auditoría, Plantillas, Notificaciones, Búsqueda global)

## 1. Objetivo de cada submódulo

- **Importación/Exportación**: alta masiva de Asset vía CSV (hasta 10.000 filas), vista previa validada antes de confirmar, exportación de listados a Excel/PDF.
- **Reportes**: paneles operativos de solo lectura — inventario, KPIs de mantenimiento (MTTR/MTBF), garantías por vencer, existencias bajas — con exportación de 3 de los 4.
- **Documentos**: adjuntar archivos a un Asset o MaintenanceOrder (allowlist de entidad, no de tipo de archivo). Metadatos en SQL, bytes en Blob Storage. Inmutable.
- **Auditoría**: registro de solo inserción de cada comando `IAuditableCommand`, éxito o fracaso.
- **Plantillas**: catálogo versionado de texto plano — solo catálogo, sin motor de renderizado/sustitución ni generación de PDF/firma todavía.
- **Notificaciones**: in-app + email best-effort, exclusivamente derivadas del ciclo de vida de una Aprobación. Se autopurgan.
- **Búsqueda global**: multi-entidad (8 tipos), filtrada por permiso de cada usuario.

## 2. Importación

**Formato**: solo CSV, máx 25 MB. Columnas fijas: AssetCategoryCode, Brand, Model, SerialNumber, Description, PhysicalCondition, OrgUnitCode, IdentificationTechnology. Columnas dinámicas: `CustomField:{Code}`. Plantilla descargable por categoría.

**Validación por fila** (acumulativa, no corta en el primer error): categoría obligatoria/existente/activa; Brand/Model obligatorios (máx 100); SerialNumber único (archivo + empresa); Description máx 500; PhysicalCondition válido; OrgUnitCode existente si se indica; IdentificationTechnology válida si se indica; campos personalizados obligatorios presentes, ninguno ajeno a la categoría. Matching case-insensitive.

**Procesamiento en background** (`ImportBatchBackgroundService` + `ImportBatchProcessor`): worker único, consumidor de `IImportQueue` (Channel en memoria local, Azure Storage Queue en producción). **Idempotencia**: nunca confía en el mensaje, solo en `ImportBatchStatus` persistido — `Queued`→valida, `Processing`→confirma, cualquier otro estado es no-op. Usa `IgnoreQueryFilters()` (sin HttpContext).

Fase validación: `Queued→Validating`, guarda TotalRows/ValidRows/InvalidRows + ReportJson, pasa a `Validated` (o `Failed` si truena).
Fase confirmación (solo si `Processing`): **re-valida todo desde cero**. Si `AllOrNothing` y sigue habiendo inválidas → no crea nada, `Failed`: "El lote tiene filas inválidas al confirmar (el estado pudo cambiar desde la vista previa); no se creó ningún activo." Si `ValidRowsOnly` (o AllOrNothing sin inválidas): crea un Asset por fila válida (folio + etiqueta), termina `Completed` o `CompletedWithErrors`.

Subir el archivo NUNCA valida ni escribe nada — solo crea el ImportBatch en `Queued`.

**Estados**: `Queued→Validating→Validated→Processing→(Completed|CompletedWithErrors)`, con salidas a `Failed` y `Cancelled` (solo desde Queued/Validating/Validated — no se puede cancelar en Processing).

**Reporte por fila**: `ReportJson` con lista `ImportRowResult(RowNumber, Success, Errors[], CreatedAssetFolio?)` — se lee como un todo, no es tabla consultable fila por fila del lado servidor.

## 3. Exportación

Dos casos, ambos síncronos (sin cola):
1. `ExportAssetsQuery` (`GET /api/v1/exports/assets`, Exports.Create): mismos filtros que el listado, tope 10.000 filas, Xlsx/Pdf. Columnas: Folio, Categoría, Marca, Modelo, Serie, Estado, Condición, Ubicación.
2. Exportaciones de Reportes (`Reports.Export`): expiring-warranties, low-stock-consumables, maintenance-kpis — reutilizan el mismo query/handler que la pantalla.

`TabularFileBuilder` común (ClosedXML para xlsx, PdfSharp para pdf, fuente embebida Liberation Sans por incompatibilidad de PdfSharp 6 con fuentes del SO en Linux). Nombres con timestamp UTC.

## 4. Auditoría

**Qué se audita**: todo comando `IAuditableCommand`, vía `AuditBehavior` (después de ValidationBehavior — solo lo que pasó autorización/validación llega a auditoría). Se audita éxito y fracaso.

Auditables en estos módulos: UploadImportBatchCommand, CommitImportBatchCommand, CancelImportBatchCommand, ExportAssetsQuery, ExportExpiringWarrantiesQuery, ExportLowStockConsumablesQuery, ExportMaintenanceKpisQuery, UploadDocumentCommand. **NO auditables**: CreateTemplateCommand, AddTemplateVersionCommand, SetTemplateActiveCommand.

**Contenido de `AuditEntry`**: CompanyId? (best-effort por reflexión, puede quedar null), UserId, UserDisplayName (denormalizado a propósito), CommandName, Module/Action, DetailsJson, Succeeded, ErrorMessage, IpAddress, UserAgent, CorrelationId, OccurredAtUtc.

**Por qué solo inserción**: rastro real "quién hizo qué cuándo" — deliberadamente no es un motor de diff campo a campo. Sin edición/borrado, solo `Create`.

**Pantalla `/audit`**: columnas Fecha, Usuario, Comando, Módulo.Acción, Resultado. Sin CompanySwitcher (permiso global tenant-wide). Filtros en UI: Comando (texto libre), Desde/Hasta (fecha) — el endpoint soporta además userId y companyId, no expuestos en la UI.

## 5. Documentos

Sin allowlist de tipo de archivo — solo se restringe el tipo de entidad: `{"Asset","MaintenanceOrder"}`. Almacenamiento: Blob Storage, contenedor único `documents`, aislamiento por prefijo `{companyId}/{entityType}/{entityId}/{Guid}-{nombreSanitizado}`. Límite 25 MB (igual que importación).

Descarga: nunca usa URL SAS con expiración — transmite (stream) a través de la propia API, revalidando permiso/acceso en cada descarga. El frontend usa un route handler propio para reenviar el `Authorization: Bearer` (el navegador no puede adjuntarlo desde un `<a href>` plano).

Inmutable: sin edición/borrado, solo `Documents.Read`/`Documents.Create`.

## 6. Notificaciones

**Disparadores únicos**: ciclo de vida de una Aprobación:
- `ApprovalRequested` → notifica a todos los titulares de rol elegible (excepto solicitante). Título: "Tienes una aprobación pendiente". Cuerpo: "Alguien solicitó una aprobación que puedes decidir. Revisa Mis aprobaciones." (simplificación V1: notifica sin respetar orden de turno secuencial).
- `ApprovalCompleted` → al solicitante: "Tu solicitud fue aprobada" / "La aprobación que solicitaste fue completada."
- `ApprovalRejected` → al solicitante: "Tu solicitud fue rechazada" / "La aprobación que solicitaste fue rechazada."

Ningún otro módulo genera notificaciones (import, documentos, auditoría, plantillas, reportes, búsqueda no notifican). Cada notificación intenta email best-effort (`NoOpEmailSender` si no hay SMTP configurado; `SmtpEmailSender` con fallo silencioso si hay).

**Retención**: `NotificationRetentionBackgroundService` — único purgado automático real del sistema. Cada 24h (configurable) borra `Notification` con más de 90 días (configurable). `Movement`, `Transfer`, `Assignment`, `SignatureRecord`, `AuditEntry` se retienen indefinidamente.

**Marcar como leída**: sin permiso RBAC — solo valida que sea el dueño.

## 7. Búsqueda global

8 tipos, cada uno filtrado por el permiso `{Módulo}.Read` del usuario:
| Entidad | Permiso | Campos buscados |
|---|---|---|
| Asset | Assets.Read | Folio, Marca, Modelo, Serie |
| Movement | Movements.Read | Folio |
| MaintenanceOrder | Maintenance.Read | Folio, Descripción |
| Warranty | Warranties.Read | Proveedor |
| SparePart | SpareParts.Read | Nombre, Serie, Número de parte |
| Consumable | Consumables.Read | Nombre, SKU |
| InternalRequest | Requests.Read | Justificación |
| User | Users.Read | Nombre, Correo (global, sin CompanyId) |

Requiere sesión; término mínimo 2 caracteres; filtra por empresas accesibles; `.Contains(term)`; máximo 8 resultados por tipo. `Movement` enlaza al Asset relacionado (sin pantalla propia de detalle).

## 8. Pantallas frontend (texto literal, hardcodeado, no en es.json)

### `/imports`
Título "Importaciones". Tabla: Archivo, Estado (badge), Filas ("{válidas} válidas / {inválidas} inválidas de {total}"), Subido. Vacío: "No hay lotes de importación todavía." Botón "Nueva importación". Error 403: "No tienes permiso para consultar importaciones (Imports.Read)."
Estados: Queued="En cola", Validating="Validando", Validated="Validado", Processing="Procesando", Completed="Completado", CompletedWithErrors="Completado con errores", Failed="Falló", Cancelled="Cancelado".

### `/imports/new`
Plantilla CSV por categoría (Select + botón "Descargar plantilla"). Archivo CSV (`accept=".csv,text/csv"`, requerido) + botón "Subir e iniciar validación". Validación: "Selecciona un archivo CSV." Error 403: "Iniciar una importación requiere permiso Imports.Create."

### `/imports/[id]`
Si en proceso: "El lote se está procesando en segundo plano. Actualiza la página para ver el avance." + botón "Actualizar". Resumen de filas. Si `Validated`: formulario de confirmación — radio "Solo filas válidas" (default) / "Todo o nada" (deshabilitado si hay inválidas). Validación: "Elige un modo de confirmación." Botón "Confirmar importación". Si cancelable: botón "Cancelar lote". Tabla "Detalle por fila": Fila, Resultado (✓/✗ + folio), Detalle.

### `/reports`
Botón alternar "Ver consolidado (todas las empresas)" ↔ "Ver por empresa". 6 StatTiles. Tarjetas: Inventario (barras por estado/categoría), Mantenimiento por categoría (+ exportar Excel/PDF), Garantías por vencer (filtro de días + exportar), Existencias bajas (+ exportar). Error consolidado: "No tienes permiso para consultar reportes consolidados (Reports.ReadConsolidated)."; por empresa: "...(Reports.Read)."

### `/audit`
Sin CompanySwitcher. Filtros: Comando, Desde, Hasta. Tabla: Fecha, Usuario, Comando, Módulo.Acción, Resultado (badge + errorMessage si falló). Vacío: "No hay entradas de auditoría todavía." Error 403: "No tienes permiso para consultar la auditoría (Audit.Read)."

### `/templates`, `/templates/new`, `/templates/[id]`
Listado: Nombre, Clave, Versión actual, Estado. Subtítulo: "Catálogo versionado de texto (resguardos, correos, notificaciones) — sin generación de documentos todavía." Nueva: Nombre, Clave, Contenido (textarea, máx 10000). Validación: "Clave, nombre y contenido son obligatorios." Detalle: Activar/Desactivar + "Agregar nueva versión" (éxito: "Versión agregada.") + historial de versiones.

### `/notifications`
Título "Mis notificaciones". Vacío: "No tienes notificaciones todavía." Cada tarjeta: título, badge "Nueva" si no leída, cuerpo, fecha, botón "Marcar como leída". **Error idéntico para 403 y error genérico**: "No fue posible consultar tus notificaciones." (aparenta bug de copy-paste, ver §11).

### `/search`
Input término + botón "Buscar". Si <2 caracteres: "Escribe al menos 2 caracteres para buscar." Resultados agrupados por tipo. Vacío: "Sin resultados para "{término}"." Error 403: "Se requiere iniciar sesión para buscar."

### Panel de Documentos (embebido)
Lista de archivos (nombre, tamaño, quién subió). Vacío: "Sin documentos todavía." Formulario de carga + botón "Cargar". Error 403: "No tienes permiso para consultar documentos (Documents.Read)."

## 9. Endpoints REST (resumen)

- **ImportBatchesController**: GET `/` `/{id}` `/template` (Imports.Read); POST `/` (Imports.Create, multipart, 25MB); POST `/{id}/commit` `/{id}/cancel` (Imports.Create).
- **ExportsController**: GET `/assets` (Exports.Create).
- **DocumentsController**: GET `/` `/{id}/content` (Documents.Read); POST `/` (Documents.Create, multipart, 25MB).
- **AuditController**: GET `/` (Audit.Read) — sin POST/PUT/DELETE.
- **NotificationsController**: GET `/mine`, POST `/{id}/read` (sin permiso, solo dueño).
- **TemplatesController**: GET `/` `/{id}` (Templates.Read); POST `/` (Templates.Create, 409 clave duplicada); POST `/{id}/versions`, PATCH `/{id}/active` (Templates.Update).
- **ReportsController**: GET `/inventory-summary`, `/maintenance-kpis` (+`/export`), `/expiring-warranties` (+`/export`), `/low-stock-consumables` (+`/export`) — Reports.Read/ReadConsolidated según companyId, Reports.Export para exportar (permiso independiente, ver §11).
- **SearchController**: GET `/` (sin permiso propio, cada tipo filtrado internamente).

## 10. Mensajes de error/éxito relevantes (selección)

"El archivo de importación debe ser un CSV (.csv)." / "El archivo no puede superar 25 MB." / "Solo un lote en cola puede comenzar a validarse." / "Solo un lote validado puede confirmarse." / "El lote tiene filas inválidas — el modo 'todo o nada' requiere que todas las filas sean válidas." / "Tipo de entidad no válido para documentos." / "Ya existe una plantilla con esa clave." / "No puedes marcar como leída la notificación de otra persona." / "Formato de exportación no soportado." / mensajes de validación de fila de importación (con columna y valor citados).

## 11. Casos especiales / edge cases

1. `es.json` no cubre estos módulos — todo hardcodeado.
2. Comandos de Templates sin `IAuditableCommand` — no queda claro si es intencional.
3. `Reports.Export` desacoplado de `Reports.Read`/`ReadConsolidated` — combinaciones inconsistentes posibles.
4. `AuditBehavior.TryGetCompanyId` es best-effort — algunos comandos generan `CompanyId=null` en su AuditEntry.
5. Mensaje idéntico 403/genérico en `/notifications` — parece error de copy-paste.
6. `AllOrNothing` puede fallar igual en el commit si el estado cambió entre vista previa y confirmación (riesgo aceptado explícitamente).
7. Sin cancelación posible una vez en `Processing`.
8. Cola local (`ChannelImportQueue`) no persistente — un reinicio del proceso pierde mensajes encolados, sin reintento automático (límite conocido de V1).
9. `AzureStorageQueueImportQueue` borra el mensaje al recibirlo, no al terminar — riesgo similar en producción.
10. Sin permiso `Templates.Delete` — una plantilla nunca se elimina, solo se desactiva.
11. Sin reintento manual expuesto para lotes `Failed`.
12. Búsqueda de Users es global sin filtro por empresa — mismo criterio que `/users`, pero vale confirmar dado el contexto multiempresa estricto de CLAUDE.md.
