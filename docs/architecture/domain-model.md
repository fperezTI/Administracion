# Modelo de dominio

> Referencia viva. Actualizar en cada incremento que agregue o cambie entidades. Ver decisiones en
> `docs/architecture/00-analysis.md` y ADRs en `docs/architecture/decisions/`.

## Agregados raíz (V1)

Estado: los marcados **[F1]** están implementados (`Domain/Identity`, `Domain/Organization`), con
migraciones aplicadas, seed de catálogos y handlers de Application detrás de RBAC. El resto se construye en
las fases indicadas en `docs/roadmap.md`.

| Agregado | Contexto | Invariantes clave |
|---|---|---|
| `Company` **[F1]** | Organization | Hasta 50 activas (verificado en `CreateCompanyCommandHandler`, no en el agregado — es una política cruzada); `TaxId` único; no se borra físicamente |
| `OrgUnit` **[F1]** | Organization | Jerárquico (`ParentOrgUnitId`), tipo desde catálogo `OrgUnitType` (11 tipos sembrados, ver ADR 0002), pertenece a una `Company`; `MoveTo` protege solo "no ser su propio padre" — el chequeo de ciclos en todo el árbol vive en `MoveOrgUnitCommandHandler` (invariante cruzado entre instancias) |
| `User` **[F1, `Anonymize` en F12]** | Identity & Access | `EntraObjectId` único; perfil creado solo en primer login válido (`User.Provision`, disparado por `CurrentUserProvisioningMiddleware`); posee `UserRole`/`UserCompany` como colecciones propias. `Anonymize(nowUtc)` (F12) borra `DisplayName`/`Email` de forma irreversible sin borrar la fila — distinto de `Deactivate()` (reversible, solo revoca acceso); no reescribe `AuditEntry.UserDisplayName` ya emitido (ver ADR 0010/0014, `docs/privacy-retention.md`) |
| `Role` / `Permission` **[F1]** | Identity & Access | No se borran físicamente (sin endpoint de borrado); `Role.Duplicate` copia la matriz de permisos; catálogo de 68 `Permission` sembrado por migración desde `PermissionCatalog` |
| `AssetCategory` **[F2]** | Catalogs | Define campos personalizados (`CustomFieldDefinition`) y reglas de obligatoriedad; 9 categorías iniciales sembradas (pedido §10) |
| `Asset` **[F2]** | Asset Registry | `CompanyId` inmutable salvo por `Transfer` completada (**[F5]** `Asset.CompleteCrossCompanyTransfer`, único método que lo reasigna); `InternalFolio` único por empresa (generado por `IFolioGenerator`, se regenera en cada transferencia — ver ADR 0007); estado gobernado por `AssetStateMachine`; concurrencia optimista vía `RowVersion` (shadow property EF, no expuesta en el dominio) |
| `AssetTag` **[F2]** | Asset Registry | `Code` (identificador legible por máquina) es **globalmente único** — deliberadamente distinto del `InternalFolio` (único solo por empresa), ver ADR 0004; reimpresión (`RecordReprint`) no cambia `Code` |
| `Movement` **[F3, extendido F5]** | Inventory Operations | Inmutable una vez `Completed`; folio único por empresa y tipo (`MovementType`: `Assignment`, `AssignmentReturn`, `Loan`, `LoanReturn`, `Relocation`, `CrossCompanyTransferOut`, `CrossCompanyTransferIn`). `FromCompanyId`/`ToCompanyId` (F5) son solo informativos — `CompanyId` sigue siendo la única empresa "dueña" de cada fila para el filtro de consulta |
| `Assignment` **[F3]** | Inventory Operations | Dos pasos: `Create` deja el activo en `Reserved` y la asignación en `PendingSignature`; `Accept` (autoservicio del destinatario, ver ADR 0005) mueve a `Assigned`. La devolución también exige firma (de quien la ejecuta, no del destinatario saliente — simplificación V1) |
| `Loan` **[F3]** | Inventory Operations | Un paso, sin firma (ver ADR 0005); tiene fecha esperada de devolución; genera `Movement` de préstamo y de devolución |
| `Transfer` **[F5]** | Inventory Operations | Cambia `CompanyId` del activo solo al completarse (motor de aprobación de F4 + salida automática + tránsito + recepción con firma — ver ADR 0007). El único agregado con dos `CompanyId` propios (`FromCompanyId`/`ToCompanyId`) y su propio filtro de consulta con OR. **No** confundir con la reubicación intra-empresa de F3 (`MovementType.Relocation`, vía `RelocateAssetCommand`), que solo cambia `CurrentOrgUnitId` dentro de la misma empresa y no requiere aprobación. Alcance V1: solo activos `InWarehouse`; siempre llega a `InWarehouse` en destino, nunca directo a `Assigned` |
| `MaintenanceOrder` **[F6]** | Maintenance | Abrir mueve el activo a `InMaintenance` (único punto de entrada); no se cierra sin resultado, condición final y evidencia (texto, ver ADR 0008) — resultado restringido a `InWarehouse`/`Damaged`, nunca `PendingDecommission` directamente (ADR 0008); checklist opcional, snapshot de ítems al abrir (`MaintenanceOrderChecklistResult`); folio propio (`MAINT-######`) |
| `MaintenanceChecklistDefinition` / `MaintenanceChecklistVersion` **[F6]** | Maintenance | Mismo patrón que `Template`/`TemplateVersion`: versiones inmutables, `Items` como colección primitiva (EF Core 8+) |
| `Warranty` **[F6]** | Maintenance | Cobertura de garantía/soporte por activo — un activo puede tener varias a lo largo de su vida; independiente de los campos planos `WarrantyStartDate/WarrantyEndDate` de `Asset` (F2, sin tocar) — ver ADR 0008 |
| `SparePart` **[F6]** | Spare Parts & Consumables | Serializada (`SerialNumber` obligatorio, único por empresa); `Status`: `InStock \| Installed \| Disposed`; historial de instalación/retiro (`SparePartInstallation`, intervalos con `Id` propio) |
| `Consumable` / `ConsumableStockMovement` **[F6]** | Spare Parts & Consumables | Existencia (`CurrentStock`) solo cambia vía `Consumable.ApplyStockMovement`, invariante "nunca negativo"; cada cambio genera además un `ConsumableStockMovement` inmutable con folio propio (`CONS-MOV-######`), agregado independiente igual que `Movement` lo es de `Asset` |
| `InternalRequest` **[F7]** | Requests | Sin fase de borrador editable — `Create` entra directo en `PendingApproval`, mismo patrón de un paso que `Transfer`/`Loan`/`MaintenanceOrder` (ver ADR 0009); `Rejected`/`Cancelled`/`Fulfilled` son los tres "cerrada" posibles. Al aprobarse (`InternalRequestApprovalReactionHandler`, mismo esqueleto que `TransferApprovalReactionHandler`), genera automáticamente el `Assignment`/`Loan`/`MaintenanceOrder` correspondiente según `Type` (`AssetAssignment \| Loan \| Maintenance`), siempre a favor de quien solicitó — no reinvoca esos comandos, reconstruye la misma secuencia de dominio directamente |
| `ApprovalFlowDefinition` / `ApprovalInstance` **[F4]** | Approvals | Genérico, referencia polimórfica (`ContextType`/`ContextId`) a la operación de negocio — no conoce nada de quién lo consume. Roles aprobadores dinámicos, no personas fijas (ver ADR 0006). Nunca sembrado por migración: un administrador lo configura en `/approval-flows` una vez que existen roles reales en su tenant. `Create` lanza `ApprovalRequested` **[F8]** (agregado retroactivamente, aditivo) para notificar a los aprobadores elegibles |
| `SignatureRecord` **[F3 simple, F4 multi-mecanismo]** | E-Signature | Inmutable, solo `Create`. Hash de integridad (SHA-256) calculado en Application sobre un payload canónico del contenido firmado + metadatos (usuario, fecha, IP, user-agent). Dos mecanismos en V1: `TypedConfirmation` (F3) y `DrawnSignature` (F4, imagen PNG capturada en `<canvas>`) — ver ADR 0005/0006 |
| `Document` **[F8]** | Documents | Asociación polimórfica controlada (`EntityType`/`EntityId`, allowlist `Asset`/`MaintenanceOrder` en V1 — ver ADR 0010); solo `Create`, sin edición ni borrado; contenido en Blob Storage (Azurite en desarrollo), servido streameado por la API (revalida permiso+empresa en cada descarga) en vez de URLs SAS |
| `Template` / `TemplateVersion` **[F4, catálogo sin renderizado]** | Templates | Versión usada en una firma/documento es inmutable — solo catálogo de texto versionado en V1, sin motor de placeholders ni generación de PDF |
| `Notification` **[F8, purgada automáticamente desde F12]** | Notifications | Fila simple por usuario (`IsRead`/`ReadAtUtc`), sin cola de reintentos — el correo es mejor esfuerzo vía `IEmailSender`, nunca bloquea ni revierte la notificación in-app (ver ADR 0010). Autoservicio puro, sin permiso `Notifications.*` (nunca se sembró uno). `NotificationRetentionBackgroundService` (F12) borra las que superan `DataRetention:NotificationRetentionDays` (90 por defecto) — ver `docs/privacy-retention.md` |
| `AuditEntry` **[F8]** | Audit | Solo inserción; sin endpoints de edición/borrado. **Sin filtro de consulta por empresa** — mismo tratamiento que `Company`/`Role`: `Audit.Read` es un permiso administrativo global, no acotado por empresa (ver ADR 0010 — `CompanyId` se registra cuando el comando auditado lo expone, pero es dato de presentación, no un límite de acceso) |
| `ImportBatch` **[F9]** | Import/Export | Solo `Asset` en V1 (ver ADR 0011). Dos fases: `Queued → Validating → Validated` (reporte fila-por-fila, nada escrito todavía) → confirmación explícita del usuario (`AllOrNothing \| ValidRowsOnly`) → `Processing → Completed \| CompletedWithErrors \| Failed`; `Cancelled` solo antes de confirmar. Procesado por `ImportBatchBackgroundService` vía `IImportQueue` (`Channel<T>` local, Azure Storage Queue en producción — mismo contrato, ver ADR 0003), idempotente sobre el propio `Status` persistido. Exportar (`Asset` a Excel/PDF) es síncrono y streameado, no usa `ImportBatch` |
| *(sin agregado nuevo)* **[F10]** | Reporting | Reportes/paneles: proyecciones de solo lectura sobre `Asset`/`MaintenanceOrder`/`Warranty`/`Consumable` ya existentes — inventario por estado/categoría, MTTR/MTBF, garantías por vencer, existencias bajas. `CompanyId` opcional en cada query: presente exige `Reports.Read` (una empresa), ausente exige `Reports.ReadConsolidated` (todas las accesibles) — ver ADR 0012. Exportación de los tres paneles accionables reutiliza `TabularFileBuilder` (extraído de `ExportAssetsQuery`, F9) |
| *(sin agregado nuevo)* **[F11]** | *(transversal, ningún contexto propio)* | Búsqueda global multi-entidad: `GlobalSearchQuery` consulta 8 tipos ya existentes (`Asset`, `Movement`, `MaintenanceOrder`, `Warranty`, `SparePart`, `Consumable`, `InternalRequest`, `User`), cada uno incluido solo si el usuario tiene el permiso `{Módulo}.Read` correspondiente (comprobado imperativamente con `IPermissionChecker`, sin un permiso `Search.*` nuevo ni bloquear el request completo — ver ADR 0013). `Document` y los catálogos administrativos quedan fuera de V1 |

## Máquina de estados de `Asset`

Estados: `InWarehouse, Reserved, Assigned, OnLoan, InTransit, InMaintenance, UnderWarranty, Damaged, Lost,
Stolen, PendingDecommission, Decommissioned, Sold, Donated, Destroyed`.

**[Implementado, F2]** `AssetStateMachine` (`Domain/Assets/AssetStateMachine.cs`) implementa el grafo
completo de abajo en código de dominio, no en configuración, para impedir estados corruptos — con pruebas
unitarias (`AssetStateMachineTests`) cubriendo transiciones válidas, inválidas y que los estados terminales
no tengan salida. `Asset.ChangeStatus` (F3) es el único punto de entrada que puede mover `Status`, y llama a
`AssetStateMachine.EnsureCanTransition` internamente — ningún comando asigna la propiedad directamente. F3
conecta `InWarehouse ↔ Reserved ↔ Assigned` y `InWarehouse ↔ OnLoan` (vía `Assignment`/`Loan`); F4 conecta
`PendingDecommission ↔ Decommissioned` y `PendingDecommission ↔ Decommissioned → {Sold, Donated, Destroyed}`
(vía `RequestAssetDecommissionCommand`/`RequestAssetDisposalCommand`, reaccionando a
`ApprovalCompleted`/`ApprovalRejected` — ver más abajo). El resto lo disparará cada fase correspondiente
(F6 Maintenance). La tabla futura
`AssetStateTransitionRule` (configurable) definirá, por transición, si requiere justificación, evidencia,
permiso específico y/o flujo de aprobación — el **grafo** de qué transición es válida en sí es fijo:

| Desde \ Hacia | Reglas |
|---|---|
| `InWarehouse` → `Reserved, Assigned, OnLoan, InTransit, InMaintenance, PendingDecommission` | Alta libre dentro de almacén; `InMaintenance` conectada **[F6]** vía `OpenMaintenanceOrderCommand` |
| `Reserved` → `InWarehouse, Assigned, OnLoan` | Cancelación de reserva vuelve a `InWarehouse` |
| `Assigned` → `InWarehouse (devolución), OnLoan, InTransit, InMaintenance, Damaged, Lost, Stolen, PendingDecommission` | Requiere firma de devolución para volver a `InWarehouse`; `InMaintenance` conectada **[F6]**, igual que desde `InWarehouse` |
| `OnLoan` → `InWarehouse (devolución), Assigned, Damaged, Lost, Stolen` | Vencimiento genera notificación, no transición automática |
| `InTransit` → `InWarehouse, Assigned` (empresa destino) | **[F5]** Usado por `Transfer` — solo la rama `InWarehouse` está conectada en V1 (`Asset.CompleteCrossCompanyTransfer`); `Assigned` queda para una fase futura que decida cerrar/trasladar la asignación activa al transferir (ver alcance de F5) |
| `InMaintenance` → `InWarehouse, UnderWarranty, PendingDecommission, Damaged` | **[F6]** Cierre de `MaintenanceOrder` (`CloseMaintenanceOrderCommand`) decide destino, pero solo entre `InWarehouse`/`Damaged` — nunca `PendingDecommission` directamente ni `UnderWarranty` (ver ADR 0008) |
| `UnderWarranty` → `InWarehouse, InMaintenance, PendingDecommission` | Grafo reservado desde F2, **sin conectar en V1** — ninguna transición entra a `UnderWarranty` todavía (ADR 0008, mismo criterio que F3 dejó `InTransit → Assigned` pendiente hasta F5) |
| `Damaged` → `InMaintenance, PendingDecommission` | No puede asignarse directamente desde `Damaged` |
| `Lost, Stolen` → `PendingDecommission` | Requiere aprobación + evidencia (sección 13/29 del pedido) — **[F4]** `RequestAssetDecommissionCommand` |
| `PendingDecommission` → `Decommissioned` | Requiere aprobación — **[F4]**, vía `ApprovalCompleted` |
| `PendingDecommission` → `InWarehouse` | **[F4]** Agregado — una solicitud de baja rechazada regresa el activo a servicio (el grafo de F2 no tenía vuelta atrás; corrección real documentada en ADR 0006, mismo patrón que ADR 0004) |
| `Decommissioned` → `Sold, Donated, Destroyed` | Cada una requiere aprobación + evidencia propia — **[F4]** `RequestAssetDisposalCommand`. Un rechazo no cambia el estado: `Decommissioned` ya era un reposo válido |
| `Decommissioned` → *(reactivación)* | **No permitido** salvo procedimiento excepcional auditado fuera del flujo estándar (sección 13 del pedido) — se modela como comando administrativo separado `ReactivateDecommissionedAsset`, con permiso exclusivo y aprobación obligatoria, nunca como transición normal |
| `Sold, Donated, Destroyed` | Estados terminales, sin transiciones salientes |

## Motor de aprobaciones — modelo **[F4]**

```
ApprovalFlowDefinition (AuditableAggregateRoot)
  Key (p.ej. "asset.decommission", "asset.disposal")
  CompanyId? (null = aplica a todas las empresas salvo que exista un override específico)
  ApproverRoleIds: Guid[] (ordenada — el orden solo importa en modo Sequential)
  RequiredApprovals: int ("N" de "N-de-M"; en Sequential siempre = ApproverRoleIds.Count)
  Mode: Sequential | Parallel
  RequiresComment: bool
  IsActive: bool (se desactiva y se crea uno nuevo para cambiar reglas — nunca se edita in-place)

ApprovalInstance (AuditableAggregateRoot)
  FlowDefinitionId, CompanyId
  ContextType (p.ej. "AssetDecommission", "AssetDisposal:Sold") / ContextId — referencia polimórfica
  RequestedByUserId
  Status: Pending | Approved | Rejected | Cancelled
  Snapshot de Mode/RequiredApprovals/ApproverRoleIds (inmutable aunque cambie la definición después)
  Steps: ApprovalStep[] (aprobador, decisión, comentario, fecha — creados según se deciden, no pre-asignados)
```

Ver ADR 0006 para por qué se eliminó `Threshold: Unanimous` del boceto original y por qué "secuencial" es
un orden entre **roles**, no entre personas. Los módulos de negocio dependen de `IApprovalCoordinator`
(puerto en Application, implementado enteramente en Application — no necesita nada de Infraestructura) y
reaccionan a los eventos de dominio `ApprovalCompleted`/`ApprovalRejected` (`AssetApprovalReactionHandler`
es el primer y único suscriptor en V1). El motor de aprobaciones no importa ensamblados de ningún otro
contexto — nunca conoce qué es un `Asset`.

**Despacho de eventos de dominio [F4]**: `AggregateRoot.Raise`/`DomainEvents`/`ClearDomainEvents` existían
desde F0 sin usarse — F4 es la primera fase que realmente los necesita. `AppDbContext.SaveChangesAsync` los
recolecta antes de guardar y los publica (vía `IPublisher` de MediatR, envueltos en
`DomainEventNotification<T>` para que Domain nunca dependa de MediatR) solo si el guardado tuvo éxito.

## Campos personalizados por categoría (F2)

`CustomFieldDefinition` (hijo de `AssetCategory`) declara campos técnicos específicos por categoría
(CPU/RAM para laptops, IMEI para celulares, …) con `DataType` (`Text`, `Number`, `Date`, `Boolean`, `Select`)
y `IsRequired`. `AssetCustomFieldValue` guarda el valor como texto por activo; `CreateAssetCommandHandler`
valida que todos los campos marcados `IsRequired` de la categoría tengan valor antes de crear el activo. No
hay validación de tipo más allá de "obligatorio" en V1 — se documenta como simplificación deliberada, no
como omisión.

## Índices previstos desde el diseño inicial (patrones de consulta esperados)

- `Asset`: `(CompanyId, Status)`, `(CompanyId, SerialNumber)` único, `(CompanyId, InternalFolio)` único.
- `Movement`: `(CompanyId, AssetId, EffectiveDate)`, `(CompanyId, FolioNumber)` único.
- `AuditEntry`: `(CompanyId, EntityType, EntityId, OccurredAtUtc)`, `(OccurredAtUtc)` para purgas/retención.
- `ConsumableStockMovement` **[F6, implementado]**: `(ConsumableId, WarehouseOrgUnitId, OccurredAtUtc)`, más
  `(CompanyId, Folio)` único; `MaintenanceOrder`: `(CompanyId, Folio)` único, `(AssetId)`; `SparePart`:
  `(CompanyId, SerialNumber)` único.

Particionamiento físico de `Movement`/`AuditEntry` por rango de fecha se evalúa en F12 cuando el volumen real
lo justifique; no se implementa prematuramente.
