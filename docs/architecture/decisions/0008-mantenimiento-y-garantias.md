# ADR 0008 — Mantenimiento: `UnderWarranty` sin conectar en V1, y `Warranty` como agregado propio

## Estado
Aceptado (F6, 2026-09-17).

## Contexto
`AssetStateMachine` reservó desde F2 los estados `InMaintenance`, `UnderWarranty` y `Damaged` con su grafo
completo de transiciones. Al construir F6 aparecieron dos ambigüedades reales, ambas resueltas aquí:

1. El grafo permite `InMaintenance ↔ UnderWarranty`, pero el pedido no detalla un flujo distinto de "enviar
   el activo a garantía con el proveedor" separado del mantenimiento correctivo/preventivo ya cubierto por
   `MaintenanceOrder`.
2. El catálogo de permisos (`PermissionCatalog.cs`, sembrado desde F1) tiene `Warranties.Read/Create/Update`
   como módulo **separado** de `Maintenance.*` — pero `Asset` ya tiene desde F2 los campos
   `WarrantyStartDate`/`WarrantyEndDate`/`SupportContract`/`SupportProvider`, editables vía
   `UpdateAssetContractualInfoCommand` bajo `Assets.Update`.

## Decisión

- **`UnderWarranty` se deja sin conectar en V1**, documentado y no omitido — mismo criterio que F3 dejó
  `InTransit → Assigned` pendiente hasta F5. `MaintenanceOrder.Type` (`Preventive | Corrective`) es una
  clasificación para reportes futuros (F10, MTTR/MTBF), no cambia a qué estado se mueve el activo: abrir
  cualquier orden mueve `InWarehouse/Assigned → InMaintenance`; cerrarla solo permite `InWarehouse`
  (reparado) o `Damaged` (no se pudo reparar) — nunca `PendingDecommission` directamente (ver más abajo) ni
  `UnderWarranty`. Conectar esa arista con un flujo real de "enviado a garantía con el proveedor" queda
  como extensión prevista para cuando el pedido lo especifique, igual que otras aristas del grafo original
  de F2 se fueron conectando fase por fase.
- **`Warranties.*` se conecta a un agregado nuevo `Warranty`** (`Domain/Maintenance/Warranty.cs`:
  `AssetId`, `Type` — fabricante/extendida/terceros —, `Provider`, `StartDate`, `EndDate`, `Terms`), no a
  los campos planos de `Asset`. La separación es real, no cosmética: un activo puede acumular **varias**
  coberturas a lo largo de su vida (garantía de fábrica y luego una extendida comprada después, por
  ejemplo), algo que un único par de fechas en `Asset` no modela — de ahí que el catálogo sembrara
  `Warranties.Create` (registrar una cobertura nueva) como acción distinta de `Warranties.Update`
  (corregir una existente). `UpdateAssetContractualInfoCommand` (F2) no se toca: sigue bajo `Assets.Update`,
  editando el resumen de garantía/soporte que ya vive directamente en la ficha del activo.
- **Cerrar una `MaintenanceOrder` nunca mueve el activo directamente a `PendingDecommission`.** Esa arista
  ya existe en el grafo (`InMaintenance → PendingDecommission`) pero entrar a ella sin pasar por
  `RequestAssetDecommissionCommand` (F4) dejaría el activo "pendiente de baja" sin ninguna
  `ApprovalInstance` real esperando decisión — un callejón sin salida, ya que hoy nada más vuelve a crear
  esa aprobación después del hecho. Si el resultado de una orden es "no se pudo reparar y debe darse de
  baja", el flujo correcto es cerrar la orden con resultado `Damaged` y luego usar el botón "Solicitar
  baja" ya existente en la ficha del activo (disponible para cualquier activo `Damaged`, F4) — sin duplicar
  lógica de aprobación dentro de Maintenance.

## Consecuencias
- `docs/architecture/domain-model.md` documenta `MaintenanceOrder`/`Warranty`/`SparePart`/`Consumable` y
  actualiza la fila de `UnderWarranty` para reflejar que sigue sin conectar, ahora explícitamente por esta
  razón (antes era simplemente "reservado").
- Los 12 permisos de F6 (`Maintenance.*`, `Warranties.*`, `SpareParts.*`, `Consumables.*`) ya estaban
  sembrados desde F1 — ninguna migración de permisos nueva en esta fase, mismo patrón que `Transfers.*`
  esperó hasta F5.
- Verificado por integración: abrir una orden con checklist mueve el activo a `InMaintenance`; cerrarla con
  `InWarehouse` lo regresa a servicio; intentar cerrar con `PendingDecommission` como resultado es
  rechazado por el validador (400) antes de tocar el dominio.
