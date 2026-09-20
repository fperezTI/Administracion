# Reporte: Módulo de Consumibles, Repuestos y Garantías

## 0. Hallazgos de completitud
1. `UpdateConsumableCommand` / `PUT /api/v1/consumables/{id}` existe en backend y hasta en el wrapper cliente `updateConsumable()`, pero **ningún formulario del frontend lo invoca** — no se puede editar nombre/SKU/unidad/mínimo de un consumible ya creado, solo registrar movimientos de stock.
2. Ninguno de los tres submódulos usa `next-intl`/`es.json` — solo dos claves de dashboard existen: `expiringWarranties`/`lowStockConsumables`.
3. No existe `DELETE` para Consumible/Repuesta/Garantía, ni permiso `*.Delete`. Para repuestos, "dar de baja" (Dispose) es irreversible.

## 1. Objetivo y relación con Assets

**Consumibles** (`Consumable`): insumos "por existencia" (tóner, cables), no identificados individualmente, solo cantidad total (`CurrentStock`). Sin FK directa a un Asset — vínculo indirecto vía `ReferenceMaintenanceOrderId` opcional en el movimiento.

**Repuestos** (`SparePart`): piezas serializadas (`SerialNumber` único). Sí tienen relación directa con Asset: `CurrentAssetId` cuando Installed, `CurrentWarehouseOrgUnitId` cuando InStock. Historial completo de instalación/retiro.

**Garantías** (`Warranty`, vive en `Domain.Maintenance`, no en su propia carpeta): cobertura N:1 vinculada a un Asset. **Comentario explícito del código**: es deliberadamente separado de los campos planos `Asset.WarrantyStartDate/EndDate/SupportContract` (editables vía `UpdateAssetContractualInfoCommand`) porque un activo puede acumular varias coberturas a lo largo de su vida. **Coexisten dos mecanismos de garantía no sincronizados**: los campos planos del Asset y el módulo Warranty independiente — el código no define cuál es "la buena" cuando ambas existen con datos distintos.

## 2. Modelo de stock/existencias

**Consumibles** — sí manejan cantidad y umbral mínimo:
- `CurrentStock` privado, solo cambia vía `Consumable.ApplyStockMovement`.
- Todo cambio de existencia genera un `ConsumableStockMovement` inmutable (misma transacción).
- Invariantes: cantidad > 0 ("La cantidad del movimiento de existencia debe ser mayor a cero."); stock nunca negativo ("Este movimiento dejaría la existencia del consumible en negativo.", 422).
- Almacén debe ser OrgUnit tipo "Almacén" de la misma empresa (409 si no: "El almacén indicado no es válido para esta empresa.").
- Reporte "existencias bajas": solo consumibles con `MinimumStock` definido y `CurrentStock <= MinimumStock`.

**Repuestos** — no manejan cantidad, manejan estado por unidad: `InStock → Installed → InStock → ... → Disposed` (solo desde InStock). No aplica reporte de bajo stock.

**Garantías** — no aplica modelo de stock.

## 3. Pantallas frontend

### `/consumables` (listado)
Columnas: Nombre (link), SKU, Existencia + unidad + badge "Bajo mínimo" (usa `<` estricto, discrepancia con el reporte que usa `<=`), Mínimo. Vacío: "No hay consumibles registrados todavía." Error 403: "No tienes permiso para consultar consumibles (Consumables.Read)." Botón "Nuevo consumible".

### `/consumables/new`
Nombre (requerido, máx 200), SKU (opcional, máx 100), Unidad de medida (requerido, máx 50), Existencia mínima (opcional, min 0). Validación cliente: "Nombre y unidad de medida son obligatorios." Botón "Registrar consumible" → redirige a detalle.

### `/consumables/[id]`
Encabezado con existencia + badge. Formulario "Registrar movimiento": Dirección (Entrada/Salida), Motivo (Existencia inicial/Compra/Consumo/Ajuste), Almacén (solo tipo Almacén), Cantidad (>0), Notas (opcional, máx 1000). Validación: "Almacén, dirección, motivo y una cantidad mayor a cero son obligatorios." Si no hay almacenes: "No hay unidades de tipo Almacén en la estructura organizacional." Historial de movimientos: Folio/Dirección/Motivo/Cantidad/Fecha. Vacío: "Sin movimientos todavía." **Sin edición del consumible** (ver §0.1).

### `/spare-parts` (listado)
Columnas: Nombre (+ número de parte, link), Número de serie, Estado (badge). Vacío: "No hay refacciones registradas todavía." Error 403: "No tienes permiso para consultar refacciones (SpareParts.Read)."

### `/spare-parts/new`
Nombre (requerido, máx 200), Número de parte (opcional, máx 100), Número de serie (requerido, máx 100), Almacén (requerido, tipo Almacén). Si no hay almacenes: mensaje con link a `/org-units`. Validación: "Nombre, número de serie y almacén son obligatorios." Unicidad de serie por empresa (409).

### `/spare-parts/[id]`
- `InStock`: formulario Instalar (select de activos **solo `InWarehouse`**) + botón "Instalar"; botón "Dar de baja" (destructivo, **sin confirmación**).
- `Installed`: formulario Retirar (select de almacén destino) + botón "Retirar".
- `Disposed`: "Esta refacción ya fue dada de baja." (sin acciones).
- Historial de instalación/retiro: `{folio} — {fecha instalación} → {fecha retiro | "actualmente instalada"}`. Si el activo referenciado ya no existe: "(activo eliminado)".

### `/warranties` (listado)
Columnas: Activo (folio), Tipo, Proveedor, Vigencia (rojo si vencida), acción "Editar". Vacío: "No hay garantías registradas todavía." Error 403: "No tienes permiso para consultar garantías (Warranties.Read)."

### `/warranties/new`
Activo (select, requerido), Tipo (Fabricante/Extendida/Terceros, requerido), Proveedor (requerido, máx 200), Inicio/Fin (fecha, requeridos), Términos (opcional, máx 2000). Validación: "Activo, tipo, proveedor y fechas son obligatorios." Regla cruzada: "La fecha de inicio de la garantía debe ser anterior o igual a la fecha de fin." Redirige a `/warranties` (no al detalle — no existe detalle individual).

### `/warranties/[id]` (edición, sin vista de solo lectura separada)
Mismos campos salvo Activo (no reasignable). Validación: "Tipo, proveedor y fechas son obligatorios." Botón "Guardar cambios".

## 4. Endpoints REST

**ConsumablesController**: GET `/` (companyId; Consumables.Read), GET `/{id}/movements` (Consumables.Read), POST `/` (Consumables.Create), PUT `/{id}` (Consumables.Update, **sin UI**), POST `/{id}/movements` (Consumables.Update, auditado; 422 cantidad≤0 o stock negativo; 409 almacén inválido). Nota: registrar movimiento usa el mismo permiso `Consumables.Update`, no uno propio.

**SparePartsController**: GET `/` (SpareParts.Read, filtro status), GET `/{id}` (incluye installations[]), POST `/` (SpareParts.Create; 409 serie duplicada o almacén inválido), POST `/{id}/install` (SpareParts.Update, auditado; 422 si no InStock; **no valida estado del Asset destino** — hallazgo de seguridad/consistencia), POST `/{id}/uninstall` (422 si no Installed), POST `/{id}/dispose` (422 si no InStock).

**WarrantiesController**: GET `/` (companyId, assetId?; Warranties.Read), POST `/` (Warranties.Create), PUT `/{id}` (Warranties.Update). **No existe GET individual** — la pantalla de edición filtra client-side sobre la lista completa.

**ReportsController** relacionados: `/expiring-warranties` (+ export), `/low-stock-consumables` (+ export) — Reports.Read/ReadConsolidated/Export, tope 1000 filas.

## 5. Reglas de negocio no obvias

1. Auditoría inconsistente: solo `RegisterConsumableStockMovementCommand`, `InstallSparePartCommand` y exportaciones de reportes son auditables. `CreateConsumableCommand`, `UpdateConsumableCommand`, `CreateSparePartCommand`, `UninstallSparePartCommand`, `DisposeSparePartCommand`, `CreateWarrantyCommand`, `UpdateWarrantyCommand` NO lo son.
2. El almacén se re-valida en cada operación (existe, tipo Almacén, misma empresa) — nunca se confía en el `companyId` del cliente.
3. `SparePart.MarkDisposed` solo permitido desde InStock: "Solo una refacción en existencia puede darse de baja (retírala primero si está instalada)."
4. El historial de instalación de un SparePart usa `IgnoreQueryFilters()` — sobrevive a que el activo haya sido transferido a otra empresa (decisión consciente, puede revelar folio de activo de otra empresa).
5. Movimientos de consumible verdaderamente inmutables — sin comando de "movimiento compensatorio" dedicado; la corrección es un nuevo movimiento con Reason=Adjustment en dirección contraria.
6. Unicidad de número de serie de SparePart es por empresa, no global.
7. Ninguna de las tres entidades permite reasignar su empresa (CompanyId privado, sin comando de transferencia).

## 6. Mensajes de error/éxito literales

**Dominio (422):** "La cantidad del movimiento de existencia debe ser mayor a cero." / "Este movimiento dejaría la existencia del consumible en negativo." / "El nombre del consumible es obligatorio." / "La unidad de medida del consumible es obligatoria." / "La existencia mínima no puede ser negativa." / "El nombre de la refacción es obligatorio." / "El número de serie de la refacción es obligatorio." / "Solo una refacción en existencia puede instalarse." / "No se encontró el registro de instalación vigente de esta refacción." / "Solo una refacción instalada puede retirarse." / "Solo una refacción en existencia puede darse de baja (retírala primero si está instalada)." / "El proveedor de la garantía es obligatorio." / "La fecha de inicio de la garantía debe ser anterior o igual a la fecha de fin."

**Conflicto (409):** "El almacén indicado no es válido para esta empresa." / "Ya existe una refacción con ese número de serie en esta empresa." / "La refacción y el activo deben pertenecer a la misma empresa." / "Formato de exportación no soportado."

**Acceso (403):** "El usuario no tiene acceso a la empresa indicada/de este consumible/de esta refacción/de este activo/de esta garantía."

**Frontend:** listados vacíos ya citados; permisos ya citados; fallbacks genéricos "No fue posible consultar/registrar/instalar/retirar/actualizar..."; validaciones cliente ya citadas.

## 7. Casos especiales / edge cases

1. Coexistencia sin conciliar entre `Asset.WarrantyStartDate/EndDate` y el módulo Warranty independiente.
2. Discrepancia `<` (badge en lista) vs `<=` (reporte de bajo stock) para "bajo mínimo".
3. Instalación de repuesto sin validar estado del activo en backend (UI restringe a InWarehouse, API no).
4. Sin endpoint de detalle (GET /{id}) para Consumable ni Warranty — resuelven client-side sobre el listado completo, sin paginación.
5. `GetConsumablesQuery`/`GetWarrantiesQuery`/`GetSparePartsQuery` sin límite de filas (a diferencia de reportes, topados en 1000).
6. Términos de garantía es texto libre hasta 2000 caracteres, sin adjuntar documento (aunque el módulo Documents existe, no está integrado aquí).
7. Redirecciones inconsistentes tras alta: Consumible/Repuesto → detalle; Garantía → listado (no existe detalle).
8. Botón "Dar de baja" de repuesto sin confirmación — irreversible.
