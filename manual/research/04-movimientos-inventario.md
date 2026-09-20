# Reporte: Módulo de Operaciones de Inventario (Movements / Transfers / Assignments / Loans)

## 0. Nota metodológica
Ninguna de estas pantallas usa `es.json` — todos los textos están hardcodeados en español en los `.tsx`, contradiciendo la convención de CLAUDE.md.

## 1. Objetivo de cada submódulo y relación con el Asset

Los cuatro submódulos son las únicas formas (junto con Maintenance) de mover el estado (`AssetStatus`) y la custodia de un `Asset`. Cada operación genera un `Movement` (bitácora inmutable):

| Submódulo | Objetivo | Movement generado | Cambia Asset.Status a |
|---|---|---|---|
| Assignment | Custodia formal de largo plazo, con firma de aceptación | Assignment (nace Pending) | Reserved (crear) → Assigned (firmar) → InWarehouse (devolver) |
| Loan | Préstamo informal de corto plazo, sin firma, con fecha esperada de devolución | Loan (nace Completed) | OnLoan (crear) → InWarehouse (devolver) |
| Transfer | Reasignar un activo a otra empresa del tenant, con aprobación + salida + tránsito + recepción firmada | CrossCompanyTransferOut + CrossCompanyTransferIn | InTransit (aprobar) → InWarehouse en destino (recibir) |
| Movement | Bitácora consolidada, solo lectura, más reubicaciones intra-empresa (Relocation) | — | Relocation no cambia Status, solo CurrentOrgUnitId |

Puntos documentados en el código: `Assignment` es deliberadamente de dos pasos (crear=Reserved, firmar=Assigned) — "simplificación V1". `Loan` es de un solo paso y sin firma. `Transfer` compone el motor genérico de Aprobaciones + Movement/firma, no introduce primitivas nuevas. La reubicación intra-empresa NO es un Transfer: no requiere aprobación y no cambia AssetStatus.

## 2. Máquinas de estado

### Transfer (`TransferStatus`)
`PendingApproval → Rejected | Cancelled | InTransit → Completed`

| Transición | Disparador | Permiso |
|---|---|---|
| → PendingApproval | RequestCrossCompanyTransferCommand | Transfers.Create |
| PendingApproval → Cancelled | CancelTransferCommand, solo el solicitante | Ninguno (protección por identidad) |
| PendingApproval → InTransit ("salida") | Automático: TransferApprovalReactionHandler reacciona a ApprovalCompleted (ContextType="CrossCompanyTransfer") | Aprobación gobernada por rol configurado en el flujo "asset.cross-company-transfer", no permiso fijo |
| PendingApproval → Rejected | Automático vía ApprovalRejected | ídem |
| InTransit → Completed ("recepción") | ReceiveCrossCompanyTransferCommand | Transfers.Update + acceso real a ToCompanyId (doble candado) |

No existe estado intermedio "aprobado pero no salido" — Depart colapsa ambos pasos.

### Assignment (`AssignmentStatus`)
`PendingSignature → Accepted → Returned`, o `PendingSignature → Cancelled`.
- Crear → PendingSignature (Assignments.Create).
- Firmar (`SignAssignmentCommand`) → Accepted, self-service, solo el destinatario, sin permiso RBAC.
- Cancelar (`CancelPendingAssignmentCommand`) → Cancelled, Assignments.Update.
- Devolver (`ReturnAssignmentCommand`) → Returned, Returns.Create.
Cada transición empuja el Asset: crear→Reserved; firmar→Assigned; devolver→InWarehouse; cancelar→InWarehouse.

### Loan (`LoanStatus`)
`Active → Returned` (sin cancelación). Crear (Loans.Create) valida fecha esperada no en el pasado. Devolver (Returns.Create).

### Movement (`MovementStatus`)
`Pending → Completed | Cancelled`. Nace Completed para Loan/LoanReturn/Relocation/CrossCompanyTransferOut/In (un solo paso); nace Pending solo para Assignment (depende de firma). Sin métodos Update/Delete — inmutabilidad real en código.
`MovementType` backend (7 valores): Assignment, AssignmentReturn, Loan, LoanReturn, Relocation, CrossCompanyTransferOut, CrossCompanyTransferIn.

## 3. Pantallas frontend

### `/movements`
Filtro: Tipo (Select, "Todos" + 5 claves — ver bug §7.8), companyId oculto vía CompanySwitcher. Tabla: Folio, Activo (link), Tipo, Fecha, Notas. Solo lectura, paginada (30/página). Vacío: "No hay movimientos con estos filtros." Error 403: "No tienes permiso para consultar movimientos (Movements.Read)."

### `/transfers` (listado)
Tabla: Folio (link, muestra assetFolio), Origen, Destino, Estado, Fecha. Botón "Nueva transferencia". Vacío: "No hay transferencias todavía." Error 403: "No tienes permiso para consultar transferencias (Transfers.Read)." Subtítulo: "Activos en tránsito o transferidos entre las empresas del tenant — aparece si tu empresa es origen o destino."

### `/transfers/new`
- Activo (obligatorio): solo `InWarehouse`. Ayuda: "No hay activos en almacén disponibles para transferir."
- Empresa destino (obligatorio): todas las empresas activas del tenant salvo la actual — **no restringido a empresas donde el usuario tenga membresía**.
- Justificación (opcional, máx 1000).
Botón "Solicitar transferencia". Validación cliente: "Selecciona el activo y la empresa destino." Éxito → redirect a `/transfers/{id}`.

### `/transfers/[id]`
Muestra folio, fromCompanyName→toCompanyName, estado, solicitante, fechas, justificación.
- "Cancelar solicitud" — solo si PendingApproval.
- Formulario de recepción — solo si InTransit: SignatureMechanismFields (TypedConfirmation/DrawnSignature) + "Confirmar recepción". Éxito: "Transferencia recibida."

### `/assignments` (listado)
Tabla: Folio, Asignado a, Estado, Fecha. Botón "Nueva asignación". Vacío: "No hay asignaciones todavía." Error 403: "No tienes permiso para consultar asignaciones (Assignments.Read)."

### `/assignments/new`
- Activo (obligatorio, InWarehouse).
- Destinatario (obligatorio) — Select de todos los usuarios, no filtrado por membresía en el propio select (validación en backend).
- Unidad organizacional (opcional, default "Sin asignar").
- Notas (opcional, máx 500).
Validación: "Selecciona el activo y el destinatario."

### `/assignments/[id]`
Folio, "Asignado a {nombre}", estado. Tarjeta "Firma de recepción" (si existe) / "Firma de devolución" (si existe). Botón "Cancelar asignación" (solo PendingSignature). Formulario devolución (solo Accepted): "Tu nombre completo" (obligatorio, máx 200) + Notas (opcional, máx 500), botón "Registrar devolución".

### `/loans` (listado)
Tabla: Folio, Prestado a, Devolución esperada, Estado. Botón "Nuevo préstamo". Vacío: "No hay préstamos todavía." Error 403: "No tienes permiso para consultar préstamos (Loans.Read)."

### `/loans/new`
Activo (obligatorio, InWarehouse), Destinatario (obligatorio), Fecha esperada de devolución (obligatorio, `min=hoy` en HTML, validación real en dominio), Notas (opcional, máx 500).

### `/loans/[id]`
Folio, "Prestado a {nombre}", estado, fechas. Formulario devolución (solo Active): **solo Notas** (sin firma, sin nombre — consistente con "sin firma" por diseño). Botón "Registrar devolución".

### `/my-assignments`
Autoservicio: todas las asignaciones del usuario. Si PendingSignature: formulario embebido "Escribe tu nombre completo para confirmar" + "Confirmar recepción". Éxito: "Recepción confirmada." Vacío: "No tienes activos asignados." Nota: mensaje 403 y error genérico son idénticos aquí ("No fue posible consultar tus asignaciones."), a diferencia de otras pantallas.

## 4. Endpoints REST

**MovementsController** (`api/v1/movements`): GET `/` (companyId, pageNumber, pageSize, assetId?, type?) — Movements.Read. Sin POST/PUT/DELETE.

**TransfersController** (`api/v1/transfers`): GET `/` (Transfers.Read), GET `/{id}` (Transfers.Read), POST `/` (Transfers.Create, 201; 403 sin acceso empresa origen; 404; 409 si activo no InWarehouse o ToCompanyId=origen; 422), POST `/{id}/receive` (Transfers.Update + acceso a ToCompanyId; 422 si no InTransit), POST `/{id}/cancel` (sin permiso, protegido por identidad; 422 si no solicitante o no PendingApproval).

**AssignmentsController** (`api/v1/assignments`): GET `/` (Assignments.Read), GET `/mine` (self-service, sin permiso), GET `/{id}` (Assignments.Read), POST `/` (Assignments.Create, 201 con MovementId/Folio; 409 destinatario sin acceso; 422 transición inválida), POST `/{id}/sign` (sin permiso, self-service; 403 si no destinatario; 422 si no PendingSignature), POST `/{id}/cancel` (Assignments.Update; 422 si no PendingSignature), POST `/{id}/return` (Returns.Create; 422 si no Accepted).

**LoansController** (`api/v1/loans`): GET `/` (Loans.Read), GET `/{id}` (Loans.Read), POST `/` (Loans.Create; 409 prestatario sin acceso; 422 fecha pasada o transición inválida), POST `/{id}/return` (Returns.Create; 422 si no Active).

**AssetsController.Relocate**: POST `/api/v1/assets/{id}/relocate` (Assets.Update; 409 si NewOrgUnitId=actual).

Comandos IAuditableCommand: CreateAssignmentCommand, CreateLoanCommand, RequestCrossCompanyTransferCommand, ReceiveCrossCompanyTransferCommand, CancelTransferCommand, RelocateAssetCommand. **NO auditables**: SignAssignmentCommand, ReturnAssignmentCommand, CancelPendingAssignmentCommand, ReturnLoanCommand.

## 5. Reglas de negocio no obvias

1. Inmutabilidad real en código (no solo docs) — sin métodos Update genéricos en ningún agregado.
2. `Asset.CompanyId` solo cambia vía `Asset.CompleteCrossCompanyTransfer`: regenera InternalFolio en la empresa destino, preserva AssetTag.Code y todo el historial, limpia CurrentOrgUnitId, fuerza status a InWarehouse.
3. La aprobación de una transferencia dispara automáticamente la "salida" (sin paso manual intermedio). Si se rechaza, el activo nunca salió de InWarehouse — nada que revertir.
4. Doble candado en la recepción: permiso RBAC `Transfers.Update` + membresía real en ToCompanyId — es el único lugar donde una transferencia está realmente protegida contra "recibirse por la empresa equivocada", ya que solicitar una transferencia permite elegir cualquier empresa del tenant sin validar membresía.
5. `GetTransfersQuery` es la única consulta que filtra por OR de dos empresas (origen o destino).
6. Devolución de Assignment exige firma de quien recibe (típicamente TI/almacén), no de quien entrega — simplificación V1.
7. Loan no tiene noción de vencimiento activo — ExpectedReturnDate es solo informativa, sin job que cambie estado.
8. Permiso `Loans.Update` existe en catálogo pero ningún comando lo usa.
9. `RelocateAssetCommand` no valida el estado del activo — se puede reubicar un activo Assigned/OnLoan; pero `Assignment.OrgUnitId` nunca se actualiza tras la reubicación, generando posible desincronización.

## 6. Mensajes de error/éxito literales

**Dominio (422):** "El folio del movimiento es obligatorio." / "Solo un movimiento pendiente puede completarse/cancelarse." / "La empresa destino debe ser distinta de la empresa de origen." / "Solo una transferencia pendiente de aprobación puede salir/rechazarse/cancelarse." / "Solo quien solicitó la transferencia puede cancelarla." / "Solo una transferencia en tránsito puede recibirse." / "Solo una asignación pendiente de firma puede aceptarse/cancelarse." / "Solo una asignación aceptada puede devolverse." / "La fecha esperada de devolución no puede ser en el pasado." / "Solo un préstamo activo puede devolverse." / "No es válido transicionar un activo de '{from}' a '{to}'." / "El nuevo folio interno es obligatorio."

**Aplicación (403/404/409):** "El usuario no tiene acceso a la empresa de este activo/esta asignación/este préstamo/la empresa indicada/destino." / "El destinatario no tiene acceso a la empresa de este activo." / "Solo un activo en almacén puede transferirse entre empresas." / "El activo ya está en esa unidad organizacional." / "Se requiere iniciar sesión para..." / "Solo el destinatario de la asignación puede firmarla."

**Frontend:** mensajes 403 por pantalla ya citados; éxitos "Transferencia recibida.", "Recepción confirmada."

## 7. Casos especiales / edge cases

1. Transferir activo no-InWarehouse: bloqueado por 409 explícito.
2. Crear segunda Assignment sobre activo ya Reserved/Assigned: bloqueado por AssetStateMachine (422), no por validación explícita del handler.
3. **Ambigüedad real**: préstamo de un activo Reserved es posible (Transitions[Reserved] incluye OnLoan) llamando al endpoint directo (la UI solo ofrece InWarehouse). Si luego el destinatario original firma esa asignación, `Assignment.Accept` no revalida el estado del activo — la firma también tendría éxito, dejando el activo "Assigned" pese a estar físicamente prestado. No hay comentario ni test que descarte esto.
4. Devolución de préstamo vencido: sin marca de atraso, sin notificación automática documentada en este módulo (`docs/architecture/domain-model.md` dice "vencimiento genera notificación", pero no se encontró el disparador en este alcance).
5. Cancelar transferencia fuera de PendingApproval: bloqueado (422).
6. Recibir transferencia por empresa equivocada: mitigado por doble candado (§5.4).
7. Reubicar activo fuera de almacén: permitido, puede desincronizar Assignment.OrgUnitId.
8. **Bug confirmado**: frontend (`api.ts`) solo declara 5 de los 7 `MovementType` — faltan CrossCompanyTransferOut/In. El filtro no los ofrece y la celda "Tipo" se renderiza vacía para esas filas.
9. Falta de auditoría en operaciones de autoservicio/devolución (Sign/Return/Cancel de Assignment, Return de Loan).
10. Permiso `Loans.Update` sin uso.
11. Toda la UI de estos módulos usa texto hardcodeado, no `next-intl`.
