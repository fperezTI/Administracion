# Reporte: Módulo de Mantenimiento (Checklists + Órdenes)

## 1. Objetivo

**`MaintenanceChecklistDefinition`**: plantilla reutilizable, versionada, de ítems de verificación (texto libre), opcionalmente asociada a una `AssetCategory`. Independiente de cualquier orden concreta.

**`MaintenanceOrder`**: ejecución concreta de un mantenimiento sobre un Asset. Al abrirse puede enlazar opcionalmente un checklist — se **copia (snapshot)** la versión más reciente dentro de la orden (`MaintenanceOrderChecklistResult`). La orden nunca referencia la definición "en vivo".

## 2. Máquina de estados de la Orden

`MaintenanceOrderStatus`: solo `Open → Closed`, sin retorno, sin cancelación/reapertura.

| Transición | Disparador | Permiso | Efecto en Asset |
|---|---|---|---|
| → Open | `OpenMaintenanceOrderCommand` (directo) o `InternalRequestApprovalReactionHandler` (aprobación de solicitud interna tipo Maintenance) | Maintenance.Create (ruta directa) | `ChangeStatus(InMaintenance)`, solo si estaba InWarehouse o Assigned |
| Open → Closed | `CloseMaintenanceOrderCommand` | Maintenance.Update | `ChangeStatus(resultStatus)`, solo InWarehouse o Damaged |

Permisos del módulo: `Maintenance.Read/Create/Update` — no existe `Maintenance.Delete`.

## 3. Estructura de un Checklist

Todos los ítems son de un único tipo: texto libre + resultado booleano + nota opcional. No hay tipos numérico/selección/rango.

- `MaintenanceChecklistDefinition`: Key (única global — ver ambigüedad §8), Name, AssetCategoryId (opcional), IsActive, colección de `MaintenanceChecklistVersion`.
- `MaintenanceChecklistVersion`: inmutable, clave compuesta (ChecklistDefinitionId, VersionNumber), `Items: IReadOnlyList<string>`.
- `MaintenanceOrderChecklistResult`: ItemIndex, ItemText (copia literal), IsCompleted (bool), Notes (opcional).

Validaciones: Key y Name obligatorios; al menos un ítem, ninguno vacío (crear checklist o agregar versión).

## 4. Pantallas frontend

Nota: casi ningún texto pasa por `es.json` (solo `openMaintenanceOrders` para un widget de dashboard).

### `/maintenance-checklists` (listado)
Columnas: Nombre (link), Clave (mono), Versión actual (v{n}), Estado. Vacío: "No hay checklists todavía." Error 403: "No tienes permiso para consultar checklists (Maintenance.Read)." Botón "Nuevo checklist".

### `/maintenance-checklists/new`
Nombre (requerido, máx 200), Clave (requerido, máx 100), Categoría de activo (opcional, "Cualquier categoría"), Ítems (textarea, uno por línea, requerido). Validación: "Clave, nombre y al menos un ítem son obligatorios." Botón "Crear checklist" → redirige al detalle.

### `/maintenance-checklists/[id]`
Encabezado + Activar/Desactivar (sin confirmación). "Agregar nueva versión" (textarea de ítems). Validación: "Debes indicar al menos un ítem." Éxito: "Versión agregada." Historial de versiones: `v{n} — {fecha}` + lista de ítems.

### `/maintenance-orders` (listado)
Requiere CompanySwitcher. Tabla: Folio (mono, link), Activo, Tipo, Estado (warning=Open, success=Closed), Abierta (fecha). Vacío: "No hay órdenes de mantenimiento todavía." Error 403: "No tienes permiso para consultar mantenimientos (Maintenance.Read)." Botón "Nueva orden".

### `/maintenance-orders/new`
Activo (select, en almacén o asignado, requerido; puede venir preseleccionado por `?assetId=`). Tipo (Preventivo/Correctivo, requerido). Checklist (opcional, "Sin checklist" default, solo activos). Descripción (textarea, requerida, máx 1000). Validación: "Activo, tipo y descripción son obligatorios." Botón "Abrir orden" → redirige al detalle.

### `/maintenance-orders/[id]`
Encabezado: folio, `{assetFolio} — {tipo}`, estado. Descripción + fecha de apertura. Si Open: card "Cerrar orden" con formulario de cierre. Si Closed: "Resultado" con estado resultante, fecha de cierre, notas, lista de ítems con ✓/✗. `DocumentsPanel` para adjuntar evidencia (entityType=MaintenanceOrder).

### Formulario de cierre
Ítems del checklist (checkbox + notas opcionales por ítem, sin validación de longitud en el comando pese a límite de 500 en BD/UI — ver §8). Resultado (select: solo "Reparado — vuelve a almacén" InWarehouse / "No se pudo reparar — queda dañado" Damaged; no ofrece PendingDecommission ni UnderWarranty). Descripción del resultado/evidencia (textarea, requerida, máx 2000). Validación: "El resultado y la descripción son obligatorios." Botón "Cerrar orden".

## 5. Endpoints REST

**MaintenanceChecklistsController**: GET `/` (Maintenance.Read), GET `/{id}` (incluye Versions[]), POST `/` (Maintenance.Create; 400 validaciones; 409 clave duplicada; 404 categoría inexistente), POST `/{id}/versions` (Maintenance.Update), PATCH `/{id}/active` (Maintenance.Update, body bool plano — atípico). **Ninguno de los tres comandos de checklist implementa `IAuditableCommand`.**

**MaintenanceOrdersController**: GET `/` (paginado; Maintenance.Read; 403 si companyId no accesible), GET `/{id}` (incluye ChecklistResults[]), POST `/` (Maintenance.Create; 409 si activo no InWarehouse/Assigned o checklist sin versiones; 404), POST `/{id}/close` (Maintenance.Update; 400 ResultStatus solo InWarehouse/Damaged; 422 si ya cerrada). **Ambos comandos de orden SÍ son auditables**, pero como no tienen propiedad CompanyId propia, el AuditEntry queda con CompanyId=null.

## 6. Reglas de negocio no obvias

1. Cerrar una orden **no exige** checklist 100% completo — ítems ausentes quedan como "no completados", como una casilla sin marcar.
2. El resultado de cierre excluye deliberadamente PendingDecommission (debe pasar por RequestAssetDecommissionCommand con su aprobación) y UnderWarranty (sin flujo conectado en V1).
3. Abrir una orden solo desde InWarehouse o Assigned — validado en el handler, no en el agregado MaintenanceOrder.
4. Segunda vía de apertura: aprobación de solicitud interna tipo Maintenance — siempre Corrective, sin checklist, `createdByUserId: null` (queda como creada "por el sistema").
5. El checklist es una entidad **global**, sin CompanyId — contradice a primera vista la regla de CLAUDE.md sobre CompanyId explícito, pero es coherente si se trata como catálogo/plantilla compartida.
6. Instalar una refacción puede referenciar opcionalmente una MaintenanceOrderId — solo valida que exista, no que esté abierta ni que sea de la misma empresa (puramente informativo).
7. La "evidencia requerida" al cerrar es solo texto (ResultNotes) — adjuntar archivos es opcional vía DocumentsPanel.
8. La versión del checklist queda congelada en la orden — agregar versión o desactivar el checklist no afecta órdenes ya abiertas.

## 7. Mensajes de error/éxito literales

**Dominio (422):** "El folio de la orden de mantenimiento es obligatorio." / "La descripción de la orden de mantenimiento es obligatoria." / "Solo una orden de mantenimiento abierta puede cerrarse." / "El resultado de una orden de mantenimiento solo puede ser 'En almacén' (reparado) o 'Dañado' (no se pudo reparar)." / "Cerrar una orden de mantenimiento requiere describir el resultado (evidencia)." / "La clave del checklist es obligatoria." / "El nombre del checklist es obligatorio." / "El checklist debe tener al menos un ítem, y ninguno puede estar vacío."

**Aplicación:** "El usuario no tiene acceso a la empresa de este activo/de esta orden de mantenimiento/indicada." / "Solo un activo en almacén o asignado puede enviarse a mantenimiento." / "Este checklist todavía no tiene ninguna versión." / "Ya existe un checklist con esa clave."

**Frontend:** listados vacíos y permisos ya citados; "Debes indicar al menos un ítem.", "Versión agregada.", "Activo, tipo y descripción son obligatorios.", "El resultado y la descripción son obligatorios.", fallbacks "No fue posible abrir/cerrar/agregar la versión...".

## 8. Casos especiales / edge cases

1. Sin validación de longitud de notas por ítem en `CloseMaintenanceOrderCommandValidator` pese al límite de 500 en BD/UI — riesgo de error de truncamiento SQL no controlado si un cliente distinto al frontend web excede el límite.
2. Clave de checklist global entre empresas — no queda claro si es deliberado o una omisión de multiempresa.
3. `AssetCategoryId` del checklist no se usa para filtrar en ningún punto (ni en "Nueva orden" ni en el backend al abrir).
4. Ítems duplicados en un checklist no están prohibidos.
5. `PATCH /maintenance-checklists/{id}/active` acepta bool plano como body — atípico frente al resto de la API.
6. Orden creada vía aprobación de solicitud interna no permite elegir tipo (siempre Corrective) ni checklist.
7. Auditoría incompleta: crear checklist, agregar versión, activar/desactivar no generan AuditEntry.
8. CompanyId nulo en auditorías de mantenimiento (ni Open ni Close command tienen esa propiedad propia).
9. `NotificationRetentionBackgroundService` no aplica a este módulo — sin política de retención para datos de mantenimiento.
10. Sin notificaciones automáticas ligadas a abrir/cerrar una orden.
11. Warranty vive en el mismo namespace de dominio (`AssetManagement.Domain.Maintenance`) pero es un submódulo separado, no confundir con Checklists/Órdenes.
