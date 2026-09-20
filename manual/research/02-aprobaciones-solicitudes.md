# Reporte: Módulo de Aprobaciones y Solicitudes

## 0. Archivos analizados

**Domain**
- `apps/api/src/AssetManagement.Domain/Approvals/ApprovalFlowDefinition.cs`
- `apps/api/src/AssetManagement.Domain/Approvals/ApprovalInstance.cs`
- `apps/api/src/AssetManagement.Domain/Approvals/ApprovalInstanceStatus.cs`
- `apps/api/src/AssetManagement.Domain/Approvals/ApprovalMode.cs`
- `apps/api/src/AssetManagement.Domain/Approvals/ApprovalStep.cs`
- `apps/api/src/AssetManagement.Domain/Approvals/ApprovalStepDecision.cs`
- `apps/api/src/AssetManagement.Domain/Approvals/Events/{ApprovalRequested,ApprovalCompleted,ApprovalRejected}.cs`
- `apps/api/src/AssetManagement.Domain/Requests/InternalRequest.cs`
- `apps/api/src/AssetManagement.Domain/Requests/InternalRequestStatus.cs`
- `apps/api/src/AssetManagement.Domain/Requests/InternalRequestType.cs`

**Application**
- `apps/api/src/AssetManagement.Application/Approvals/*.cs` (10 archivos)
- `apps/api/src/AssetManagement.Application/Requests/*.cs` (6 archivos)
- `apps/api/src/AssetManagement.Application/Common/Security/PermissionCatalog.cs`
- `apps/api/src/AssetManagement.Application/Common/Behaviors/AuthorizationBehavior.cs`
- `apps/api/src/AssetManagement.Application/Common/Security/SignatureFactory.cs`
- `apps/api/src/AssetManagement.Application/Dashboards/GetExecutiveDashboardQuery.cs`

**Api**
- `Controllers/V1/ApprovalsController.cs`, `ApprovalFlowDefinitionsController.cs`, `InternalRequestsController.cs`

**Web**
- `apps/web/src/app/approval-flows/**`, `approvals/[id]/**`, `my-approvals/**`, `requests/**`, `my-requests/**`
- `apps/web/src/lib/api.ts`, `apps/web/src/lib/approval-labels.ts`, `apps/web/src/lib/request-labels.ts`
- `apps/web/src/components/layout/nav-links.ts`, `app-nav.tsx`, `signature-mechanism-fields.tsx`
- `apps/web/messages/es.json`

---

## 1. Objetivo del módulo

**`ApprovalFlowDefinition`** (`Domain/Approvals/ApprovalFlowDefinition.cs`) es la *configuración* de un tipo de aprobación: define, para una `Key` textual (p. ej. `internal-request.loan`, `asset.decommission`), qué roles pueden aprobar (`ApproverRoleIds`), cuántas aprobaciones se requieren (`RequiredApprovals`), en qué modo (`Sequential`/`Parallel`) y si exige comentario obligatorio al solicitar (`RequiresComment`). El comentario XML del propio código lo explicita: *"Approvers are a set of roles, not fixed people: membership is dynamic, resolved at decision time against whoever currently holds the role"*.

**`ApprovalInstance`** es una *ejecución concreta* de esa definición: se crea con una copia ("snapshot") de las reglas del flujo vigentes en ese momento (`ApproverRoleIds`, `RequiredApprovals`, `Mode`), de modo que si el flujo se desactiva/reconfigura después, las instancias en curso no se ven afectadas (documentado explícitamente: *"Editing an existing flow's rules never happens — deactivate and create a new one, so in-flight ApprovalInstances... are never retroactively affected"*).

La relación con el negocio es **polimórfica y genérica**: `ApprovalInstance` tiene `ContextType` (string) y `ContextId` (Guid) y **no sabe nada** de qué está aprobando — el comentario del código lo dice literalmente: *"This aggregate has zero knowledge of what it is approving — the requesting module... reacts to ApprovalCompleted/ApprovalRejected instead"*. Es el mismo motor genérico que usan `asset.decommission`, `asset.disposal` (vistos en `Domain.Approvals` pero consumidos desde otros módulos) y `InternalRequest`.

**`InternalRequest`** (Requests) es el "front door" de autoservicio: un empleado pide algo (asignación de un activo, préstamo, o mantenimiento) y **automáticamente** se dispara una `ApprovalInstance` para el flujo correspondiente. `InternalRequest` **no reemplaza** a `Assignment`/`Loan`/`MaintenanceOrder`: al aprobarse, un handler reconstruye directamente esos agregados (ver §7). El comentario en código aclara: *"Does not introduce new primitives — approving one triggers the same domain sequence Assignment/Loan/MaintenanceOrder already use"*.

Relación 1-a-1 típica: `InternalRequest.Create` → `ApprovalCoordinator.RequestApprovalAsync` → crea una `ApprovalInstance` con `ContextType = "InternalRequest"`, `ContextId = internalRequest.Id`.

---

## 2. Máquinas de estado

### 2.1 `InternalRequestStatus` (`Domain/Requests/InternalRequestStatus.cs`)
```
PendingApproval → Rejected
PendingApproval → Cancelled
PendingApproval → Fulfilled
```
- **No hay fase de borrador.** `Create()` entra directo en `PendingApproval` (comentario explícito en el código: *"No draft-editing phase... Create goes straight to InternalRequestStatus.PendingApproval"*).
- `Reject()`: solo válido si `Status == PendingApproval`; si no, `DomainException("Solo una solicitud pendiente de aprobación puede rechazarse.")`.
- `Cancel(callerUserId, ...)`: solo válido si `Status == PendingApproval` y `callerUserId == RequestedByUserId`; si no: `DomainException("Solo una solicitud pendiente de aprobación puede cancelarse.")` o `DomainException("Solo quien solicitó puede cancelar su propia solicitud.")`.
- `Fulfill(fulfillmentReferenceId, ...)`: solo válido si `Status == PendingApproval`; error: `DomainException("Solo una solicitud pendiente de aprobación puede cumplirse.")`.
- Los tres estados terminales (`Rejected`, `Cancelled`, `Fulfilled`) no tienen transición de salida.

### 2.2 `ApprovalInstanceStatus` (`Domain/Approvals/ApprovalInstanceStatus.cs`)
```
Pending → Approved
Pending → Rejected
Pending → Cancelled
```
Todas terminales excepto `Pending`. La transición se decide en `ApprovalInstance.Decide(...)`:
- Si `decision == Rejected` → `Status = Rejected` (basta **una sola** decisión de rechazo, sin importar el modo).
- Si `decision == Approved` y el conteo de pasos `Approved` alcanza `RequiredApprovals` → `Status = Approved`.
- Si es `Approved` pero aún no llega al umbral, el estado **sigue `Pending`** (queda esperando más decisiones).

`Cancel(callerUserId, ...)`: solo si `Status == Pending` y `callerUserId == RequestedByUserId`.

### 2.3 Definición de un flujo (`ApprovalFlowDefinition.Create`)
Campos: `Key`, `CompanyId?` (null = flujo global para todas las empresas), `ApproverRoleIds` (lista de `RoleId`), `RequiredApprovals` (int), `Mode` (`Sequential`|`Parallel`), `RequiresComment` (bool).

Validaciones de dominio (mensajes literales):
- `Key` vacío → `"La clave del flujo de aprobación es obligatoria."`
- `ApproverRoleIds` vacío → `"Un flujo de aprobación necesita al menos un rol aprobador."`
- `RequiredApprovals < 1` → `"El número de aprobaciones requeridas debe ser al menos 1."`
- `Mode == Sequential && RequiredApprovals != ApproverRoleIds.Count` → `"En modo secuencial se requiere una aprobación por cada rol, en orden."`

**No hay condiciones** (reglas tipo "si el monto > X entonces...") en el modelo — el flujo se resuelve únicamente por `Key` + `CompanyId` (empresa específica tiene prioridad sobre el flujo global con el mismo `Key`, ver `ApprovalCoordinator`).

**Modo Secuencial**: el orden de `ApproverRoleIds` importa. `ApprovalInstance.Decide` calcula `currentStepIndex = pasos ya Approved`, y el rol correspondiente (`ApproverRoleIds[currentStepIndex]`) es el único elegible para decidir en ese turno; si no coincide: `DomainException("Todavía no es el turno del rol correspondiente en este flujo secuencial.")`.

**Modo Paralelo**: cualquier tenedor de cualquiera de los roles listados puede decidir, en cualquier orden, hasta completar `RequiredApprovals`. Nota de dominio importante: en paralelo, `RequiredApprovals` **no** está acotado por `ApproverRoleIds.Count` — el propio comentario del código lo aclara con un ejemplo: *"roles=[Manager], requiredApprovals=3 legitimately asks for three distinct managers to each approve"*.

**Qué dispara la creación de una `ApprovalInstance`**: cualquier módulo que llame a `IApprovalCoordinator.RequestApprovalAsync(companyId, flowKey, contextType, contextId, requestedByUserId, comment, ct)`. En este alcance, es `CreateInternalRequestCommandHandler`, que mapea el `InternalRequestType` a una `flowKey` fija:
```
AssetAssignment → "internal-request.asset-assignment"
Loan            → "internal-request.loan"
Maintenance     → "internal-request.maintenance"
```
Si no existe un flujo activo con esa `Key` (ni específico de empresa ni global): `ConflictException("No hay un flujo de aprobación configurado para '{flowKey}'. Pide a un administrador que configure uno en Aprobaciones.")` — **es decir, sin flujo configurado, no se puede crear la solicitud**.

### 2.4 Reglas de decisión (`ApprovalInstance.Decide`), en orden de evaluación
1. `Status != Pending` → `"Esta aprobación ya no está pendiente."`
2. `deciderUserId == RequestedByUserId` → `"Quien solicita una aprobación no puede decidir sobre su propia solicitud."`
3. Ya existe un `ApprovalStep` de ese usuario → `"Ya registraste una decisión para esta aprobación."`
4. `decision == Rejected && comentario vacío` → `"Rechazar una aprobación requiere indicar el motivo."`
5. Ninguno de los roles del decisor está en `ApproverRoleIds` → `"No tienes un rol elegible para decidir sobre esta aprobación."`
6. (Solo secuencial) no es su turno → `"Todavía no es el turno del rol correspondiente en este flujo secuencial."`

---

## 3. "approvals" (vista general) vs "my-approvals" (bandeja personal)

**Hallazgo relevante:** en este alcance **no existe una pantalla de lista general/administrativa de aprobaciones**. No hay `GetApprovalInstancesQuery` (paginada, tipo la que sí existe para `Transfers`, `Assignments`, etc.), y en el frontend solo existen:
- `apps/web/src/app/approvals/[id]/page.tsx` — **solo detalle por id**, sin lista.
- `apps/web/src/app/my-approvals/page.tsx` — bandeja personal.

Es decir, "approvals" **no** es una vista de administración con listado — solo es la ruta de **detalle** (`/approvals/{id}`), alcanzable únicamente desde el link "Ver detalle" de `/my-approvals`, o potencialmente desde otras pantallas que enlacen una aprobación (transferencias, bajas de activos, etc., fuera de este alcance).

| | `GET /api/v1/approvals/{id}` (detalle) | `GET /api/v1/approvals/mine` (bandeja) |
|---|---|---|
| Query | `GetApprovalInstanceByIdQuery` | `GetMyPendingApprovalsQuery` |
| Autorización | `IRequiresPermission` → **`Approvals.Read`** (permiso RBAC global) | **Sin permiso** — autoservicio puro |
| Scoping | Por `Id` únicamente. **No valida `CompanyId`** contra `currentCompany.AccessibleCompanyIds`. Ver §9. | Filtra: `Status == Pending`, `CompanyId` accesible al usuario, `RequestedByUserId != userId`, y solo si el usuario tiene **al menos un rol** que sea elegible en ese momento (`IsMyTurn`, replicando la lógica de turno secuencial/paralelo) |
| Quién ve qué | Cualquiera con el permiso `Approvals.Read` puede ver el detalle de **cualquier** aprobación por id (de cualquier empresa a la que tenga acceso — y, por el hallazgo anterior, potencialmente de cualquier empresa) | Solo lo que le corresponde decidir a **ese usuario específico**, según los roles RBAC que tiene asignados (`UserRoles`), no según un permiso |

**Distinción conceptual clave**: la elegibilidad para *decidir* (aprobar/rechazar) es por **rol de negocio** (`ApproverRoleIds`, dinámico), no por **permiso RBAC**. De hecho, `ApproveApprovalStepCommand`/`RejectApprovalStepCommand` **no implementan `IRequiresPermission`** — el comentario lo dice explícitamente: *"Self-service... no IRequiresPermission — eligibility is a dynamic role/turn check performed by ApprovalInstance.Decide, not an RBAC permission"*. En cambio, ver el **detalle** de una aprobación por id sí exige el permiso `Approvals.Read`.

**Consecuencia práctica no obvia**: un usuario puede ser perfectamente elegible para aprobar (aparece en `/my-approvals` y puede pulsar "Aprobar"/"Rechazar" ahí mismo) pero **no tener el permiso `Approvals.Read`** — en ese caso, si hace clic en "Ver detalle" (que enlaza a `/approvals/{id}`), recibirá un 403.

También existen los permisos `Approvals.Approve` y `Approvals.Reject` en `PermissionCatalog.cs`, pero **no son usados por ningún comando/query/controller en todo el código** — son códigos de permiso "sembrados" pero sin efecto funcional actual.

---

## 4. "requests" vs "my-requests"

| | `GET /api/v1/requests` (`requests` — vista de módulo) | `GET /api/v1/requests/mine` (`my-requests` — bandeja personal) |
|---|---|---|
| Query | `GetInternalRequestsQuery(CompanyId, PageNumber, PageSize, Status?)` | `GetMyInternalRequestsQuery` |
| Autorización | `IRequiresPermission` → **`Requests.Read`** | **Sin permiso** — autoservicio (comentario: *"every request the caller made, regardless of RBAC permission — same self-scoping precedent as GetMyAssignmentsQuery"*) |
| Alcance | Todas las solicitudes **de una empresa** (`CompanyId` obligatorio como query param), con filtro opcional por `Status`, paginado; valida `currentCompany.AccessibleCompanyIds.Contains(request.CompanyId)` | Solo las solicitudes **creadas por el usuario actual** (`RequestedByUserId == userId`), sin importar empresa, sin paginar (lista completa) |
| Uso típico | Un administrador/gestor de la empresa revisa todas las solicitudes de todos los empleados | El empleado revisa/cancela sus propias solicitudes |
| Acciones disponibles en frontend | Solo lectura (tabla, sin botones de acción) | Botón "Cancelar" si `Status === "PendingApproval"` |

`GET /api/v1/requests/{id}` (`GetInternalRequestByIdQuery`) también exige `Requests.Read`, pero — igual que en Aprobaciones — **no valida `CompanyId`** contra el acceso del usuario (ver §9).

---

## 5. Pantallas frontend — campos, validaciones y acciones

> **Nota metodológica**: revisé `apps/web/messages/es.json` completo. Solo contiene los namespaces `App`, `Home`, `Dashboard`, `Theme`. La **única** clave relacionada con este módulo es `Dashboard.pendingApprovals = "Aprobaciones pendientes"` (usada en el KPI del dashboard ejecutivo, no en las pantallas de Aprobaciones/Solicitudes). **Todo el texto de las pantallas de este módulo está codificado directamente en español dentro de los `.tsx`**, no vía `next-intl`/`es.json`.

### 5.1 `/approval-flows` (`apps/web/src/app/approval-flows/page.tsx`)
- Título: **"Flujos de aprobación"** / subtítulo: **"Qué roles deben aprobar cada tipo de operación (p. ej. baja de activos)."**
- Tabla con columnas: Clave, Alcance (`"Esta empresa"` si `companyId` no nulo / `"Todas las empresas"` si nulo), Roles aprobadores (unidos con `" → "` si `Sequential`, con `", "` si `Parallel`), Modo (`APPROVAL_MODE_LABELS`: `Sequential → "Secuencial"`, `Parallel → "Paralelo"`), Requeridas, Estado (badge `"Activo"`/`"Inactivo"`), botón de acción.
- Vacío: **"No hay flujos de aprobación configurados todavía."**
- Botón **"Nuevo flujo"** → navega a `/approval-flows/new`.
- Botón por fila: **"Desactivar"**/**"Activar"** (según `isActive`) → `toggleApprovalFlowActiveAction` → `PATCH /api/v1/approval-flows/{id}/active`.
- Error 403: **"No tienes permiso para consultar flujos de aprobación (Approvals.Read)."** / otro error: **"No fue posible consultar los flujos de aprobación."**

### 5.2 `/approval-flows/new` (formulario de creación)
Campos (`create-approval-flow-form.tsx`):
| Campo | Tipo | Obligatorio | Detalle |
|---|---|---|---|
| Clave (`key`) | texto | Sí (`required`, `maxLength=100`) | placeholder `"p. ej. asset.decommission"` |
| Alcance (`companyId`) | select | No (default `""` = todas las empresas) | opciones: `"Todas las empresas"` + una por cada empresa del usuario (`me.companies`) |
| Modo (`mode`) | select | Sí (default `Parallel`) | `"Paralelo (cualquier orden)"` / `"Secuencial (en el orden de la lista)"` |
| Aprobaciones requeridas (`requiredApprovals`) | número, min 1 | Sí | **Si modo = Secuencial, se fuerza a `roleSlots.length` y el input queda `readOnly`**, con texto de ayuda: **"En modo secuencial se exige una aprobación por cada rol listado."** |
| Roles aprobadores (`approverRoleIds`, múltiples selects dinámicos) | select por slot | Sí, al menos uno | Botones "+ Agregar rol" / "Quitar" (solo si hay más de un slot); en modo secuencial se numeran (1., 2., ...) |
| Exigir justificación al solicitar (`requiresComment`) | checkbox | No | — |

Validación cliente (en `createApprovalFlowAction`, antes de llamar al API): si `!key || approverRoleIds.length === 0` → **"La clave y al menos un rol aprobador son obligatorios."** Si el API responde error: se muestra `error.detail` del backend o, si no hay detalle, **"No fue posible crear el flujo de aprobación."**

Botón: **"Crear flujo"** (deshabilitado mientras `pending`, texto **"Guardando…"**). Al éxito: redirige a `/approval-flows`.

Caso sin roles activos en la empresa: **"No hay roles activos todavía. Crea al menos un rol en Roles antes de configurar un flujo."** (con link a `/roles`).

Error 403 al cargar la página: **"Configurar un flujo requiere también poder consultar roles (Roles.Read)."**

### 5.3 `/approvals/[id]` (detalle de una aprobación)
- Breadcrumb: "Mis aprobaciones" → nombre de contexto (vía `describeApprovalContext`).
- `describeApprovalContext(contextType)` traduce: `"AssetDecommission" → "Baja de activo"`; `"AssetDisposal:{Sold|Donated|Destroyed}" → "Disposición de activo (venta|donación|destrucción)"`; `"InternalRequest" → "Solicitud interna"`; cualquier otro valor se muestra tal cual (fallback sin traducir).
- Encabezado: nombre del contexto, **"Solicitado por {nombre} — {Modo}, {N} aprobación(es) requerida(s)"** (pluralización manual: agrega "es"/"s" si `requiredApprovals !== 1`).
- Badge de estado: `APPROVAL_STATUS_LABELS` → `Pending: "Pendiente"`, `Approved: "Aprobada"`, `Rejected: "Rechazada"`, `Cancelled: "Cancelada"`.
- Tarjeta "Justificación" (solo si `approval.comment` no es null).
- Tarjeta "Decisiones": lista de `steps`, cada uno: **"{Nombre} — {Aprobó|Rechazó} el {fecha/hora localizada es-MX}"**, y el comentario entre comillas tipográficas si existe. Si no hay decisiones: **"Todavía no hay decisiones registradas."**
- **Sin botones de acción en esta pantalla** — ni aprobar/rechazar ni cancelar (esas acciones solo están en `/my-approvals`).
- 404 → `notFound()` de Next.js si el id no existe.

### 5.4 `/my-approvals` (bandeja personal)
- Título **"Mis aprobaciones"** / subtítulo **"Solicitudes pendientes donde tienes un rol elegible para decidir."**
- Vacío: **"No tienes aprobaciones pendientes."**
- Por cada aprobación: contexto (`describeApprovalContext`), **"Solicitado por {nombre} — {fecha}"**, comentario entre comillas si existe, link **"Ver detalle"** → `/approvals/{id}`, y el formulario de decisión (`DecideApprovalForm`).
- Error: **"No fue posible consultar tus aprobaciones."** (mismo texto para 403 y cualquier otro error).

**`DecideApprovalForm`** (`decide-approval-form.tsx`) — máquina de estados local `idle | approve | reject`:
- Estado inicial: dos botones **"Aprobar"** / **"Rechazar"**.
- Modo **Aprobar**: muestra `SignatureMechanismFields` (ver 5.6) + botones **"Cancelar"** (vuelve a idle) / **"Confirmar aprobación"** (texto **"Enviando…"** mientras pendiente).
- Modo **Rechazar**: campo **"Motivo del rechazo"** (`Input`, `required`, `maxLength=1000`) + `SignatureMechanismFields` + botones **"Cancelar"** / **"Confirmar rechazo"** (variante `destructive`).
- Validación cliente en `rejectAction`: si `!input.comment` → **"Escribe el motivo del rechazo."** (antes de llamar al API).
- Al éxito (cualquiera de las dos acciones): **"Decisión registrada."** (reemplaza todo el formulario) y hace `revalidatePath("/my-approvals")`.
- Error de API: se muestra `error.detail` o, en su defecto, **"No fue posible aprobar."** / **"No fue posible rechazar."**

### 5.5 Endpoint/función de cliente no usada — `cancelApprovalInstance`
`apps/web/src/lib/api.ts` define `cancelApprovalInstance(accessToken, approvalInstanceId)` → `POST /api/v1/approvals/{id}/cancel`, y el backend implementa completamente `CancelApprovalInstanceCommand`. **Sin embargo, no encontré ningún botón ni acción de servidor en el frontend que la invoque**. Es decir: existe endpoint + wrapper de cliente, pero **no hay UI conectada a esa función**. Ver §9 para el impacto.

### 5.6 `SignatureMechanismFields` (componente compartido, `apps/web/src/components/signature-mechanism-fields.tsx`)
Radios: **"Escribir mi nombre"** (`TypedConfirmation`, default) / **"Dibujar firma"** (`DrawnSignature`).
- Si `TypedConfirmation`: campo **"Nombre completo"** (`Input`, `required`, `maxLength=200`, name=`typedFullName`).
- Si `DrawnSignature`: componente `SignatureCanvas` (name=`signatureImageDataUrl`), sin campo de nombre (se usa el nombre de la cuenta automáticamente en backend).

### 5.7 `/requests` (lista de solicitudes de la empresa)
- Título **"Solicitudes internas"** / subtítulo **"Solicitudes de asignación, préstamo o mantenimiento hechas por cualquier persona de la empresa."**
- `CompanySwitcher` para cambiar de empresa (query param `companyId`); si el usuario no tiene empresas: `EmptyCompanyState`.
- Tabla: Activo (folio, con link a `/requests/{id}`), Tipo (`INTERNAL_REQUEST_TYPE_LABELS`: `AssetAssignment: "Asignación de activo"`, `Loan: "Préstamo"`, `Maintenance: "Mantenimiento"`), Solicitante (oculto en `sm`), Estado (badge), Fecha (oculta en `sm`).
- Vacío: **"No hay solicitudes todavía."**
- Botón **"Nueva solicitud"** → `/requests/new?companyId={companyId}`.
- Error 403: **"No tienes permiso para consultar solicitudes (Requests.Read)."** / otro: **"No fue posible consultar las solicitudes."**
- **Sin acciones** (solo lectura; las decisiones de aprobación ocurren siempre en `/my-approvals`, no en `/requests`).

### 5.8 `/requests/new` (crear solicitud) — `create-request-form.tsx`
| Campo | Tipo | Obligatorio | Detalle |
|---|---|---|---|
| Tipo de solicitud (`type`) | select | Sí (`required`) | Opciones = `INTERNAL_REQUEST_TYPE_LABELS` |
| Activo (`assetId`) | select | Sí (`required`) | Lista filtrada dinámicamente: si `type === "Maintenance"` → activos `InWarehouse` + `Assigned`; si no → solo `InWarehouse`. Etiqueta del campo cambia: **"Activo (en almacén o asignado a ti)"** vs **"Activo (debe estar en almacén)"**. Si no hay elegibles: **"No hay activos elegibles para este tipo de solicitud."** |
| Fecha esperada de devolución (`expectedReturnDate`) | date | Sí, **solo si** `type === "Loan"` (el campo ni siquiera se renderiza si no) | — |
| Justificación (`justification`) | textarea | Sí (`required`, `maxLength=1000`) | placeholder **"Explica por qué haces esta solicitud"** |

Validación cliente (`createRequestAction`):
- `!type || !assetId || !justification` → **"Tipo, activo y justificación son obligatorios."**
- `type === "Loan" && !expectedReturnDate` → **"Una solicitud de préstamo requiere la fecha esperada de devolución."**
- Error de API: `error.detail` o **"No fue posible enviar la solicitud."**

Botón: **"Enviar solicitud"** (**"Enviando…"** mientras pendiente). Al éxito: redirige a `/my-requests`.

### 5.9 `/requests/[id]` (detalle de solicitud)
- Botón **"← Volver"** a `/requests`.
- Encabezado: folio del activo, **"{Tipo} — solicitada por {nombre}"**, badge de estado.
- Tarjeta "Justificación": texto completo (`whitespace-pre-wrap`), **"Devolución esperada: {fecha}"** si aplica, **"Solicitada el {fecha}"** y, si `decidedAtUtc` existe, **" — decidida el {fecha}"**.
- **Sin acciones** en esta vista (ni cancelar ni ver la aprobación vinculada — no hay link hacia `/approvals/{approvalInstanceId}` desde aquí).
- 404 → `notFound()`.

### 5.10 `/my-requests` (bandeja personal de solicitudes)
- Título **"Mis solicitudes"** / subtítulo **"Solicitudes internas que has hecho — asignación, préstamo o mantenimiento."**
- Vacío: **"No has hecho ninguna solicitud todavía."**
- Por cada solicitud: folio (link a `/assets/{assetId}`, **no** a `/requests/{id}`), tipo, badge de estado, justificación completa, fecha de devolución esperada si aplica.
- Botón **"Cancelar"** — **solo visible si `r.status === "PendingApproval"`** → `cancelMyRequestAction` → `POST /api/v1/requests/{id}/cancel` → `revalidatePath("/my-requests")`.
- Botón **"Nueva solicitud"** → `/requests/new`.
- Error: **"No fue posible consultar tus solicitudes."** (idéntico en ambas ramas del switch).

---

## 6. Endpoints REST completos

### 6.1 `ApprovalsController` (`/api/v1/approvals`) — clase con `[Authorize]`
| Método | Ruta | Body | Command/Query | Autorización efectiva | Respuestas |
|---|---|---|---|---|---|
| GET | `/mine` | — | `GetMyPendingApprovalsQuery` | Solo requiere estar autenticado; **sin `IRequiresPermission`** — autoservicio por rol | 200 `IReadOnlyList<MyPendingApprovalSummary>` |
| GET | `/{approvalInstanceId:guid}` | — | `GetApprovalInstanceByIdQuery` | `[Authorize]` + `IRequiresPermission = Approvals.Read` | 200 `ApprovalInstanceDetail`; 404 si no existe; 403 si falta el permiso |
| POST | `/{approvalInstanceId:guid}/approve` | `DecideApprovalRequest { comment?, signatureMechanism, typedFullName?, signatureImageDataUrl? }` | `ApproveApprovalStepCommand` | `[Authorize]`; **sin `IRequiresPermission`** — elegibilidad por rol validada en `ApprovalInstance.Decide` | 204; 403 si no autenticado / empresa no accesible; 404 si no existe; 409/400 por reglas de dominio |
| POST | `/{approvalInstanceId:guid}/reject` | idéntico | `RejectApprovalStepCommand` | igual que approve | igual, más: comentario vacío → error de validación |
| POST | `/{approvalInstanceId:guid}/cancel` | — | `CancelApprovalInstanceCommand` | `[Authorize]`; sin `IRequiresPermission` — solo el solicitante original | 204; 403 si no es el solicitante o no autenticado; 404; 409 si no está `Pending` |

Validaciones de comando: `ApproveApprovalStepCommandValidator`: `ApprovalInstanceId` no vacío; `SignatureMechanism` no vacío; `Comment` máx. 1000 caracteres. `RejectApprovalStepCommandValidator`: igual, más `Comment` **obligatorio** (`NotEmpty`).

### 6.2 `ApprovalFlowDefinitionsController` (`/api/v1/approval-flows`) — `[Authorize]`
| Método | Ruta | Body | Command/Query | Autorización | Respuestas |
|---|---|---|---|---|---|
| GET | `/` | — | `GetApprovalFlowDefinitionsQuery` | `IRequiresPermission = Approvals.Read` | 200 lista (incluye activos e inactivos, sin filtro) |
| POST | `/` | `CreateApprovalFlowDefinitionCommand` | mismo | `IRequiresPermission = Approvals.Configure` | 201 `Guid`; 403; 409 si ya existe un flujo activo con esa `Key`+`CompanyId`, o si algún `RoleId` no existe |
| PATCH | `/{flowDefinitionId:guid}/active` | `bool` | `SetApprovalFlowDefinitionActiveCommand` | `IRequiresPermission = Approvals.Configure` | 204; 404 si no existe el flujo |

### 6.3 `InternalRequestsController` (`/api/v1/requests`) — `[Authorize]`
| Método | Ruta | Body / Query | Command/Query | Autorización | Respuestas |
|---|---|---|---|---|---|
| GET | `/` | query: `companyId`, `pageNumber=1`, `pageSize=50`, `status?` | `GetInternalRequestsQuery` | `IRequiresPermission = Requests.Read` | 200 `PagedResult`; 403 si `companyId` no accesible |
| GET | `/mine` | — | `GetMyInternalRequestsQuery` | sin permiso — autoservicio | 200 |
| GET | `/{internalRequestId:guid}` | — | `GetInternalRequestByIdQuery` | `IRequiresPermission = Requests.Read` | 200; 404 |
| POST | `/` | `CreateInternalRequestCommand { Type, AssetId, Justification, ExpectedReturnDate? }` | mismo | `IRequiresPermission = Requests.Create` | 201; 404 si el activo no existe; 403; 409 si el activo no está elegible o el flujo no está configurado |
| POST | `/{internalRequestId:guid}/cancel` | — | `CancelInternalRequestCommand` | sin permiso — solo el solicitante original | 204; 403 si no es el solicitante; 404; 409 si no está `PendingApproval` |

Regla de elegibilidad del activo (en el handler):
```csharp
var eligible = request.Type == InternalRequestType.Maintenance
    ? asset.Status is AssetStatus.InWarehouse or AssetStatus.Assigned
    : asset.Status == AssetStatus.InWarehouse;
```
Mensajes: *"Solo un activo en almacén o asignado puede reportarse a mantenimiento."* / *"Solo un activo en almacén puede solicitarse."*

---

## 7. Reglas de negocio no obvias

1. **Roles dinámicos, no personas fijas.** `ApproverRoleIds` se resuelve contra `UserRoles` en el momento de decidir, no al crear el flujo.
2. **El solicitante nunca puede autoaprobarse**, sin excepción.
3. **Rechazo termina el proceso de inmediato** — basta **una sola** decisión `Rejected`.
4. **Rechazo exige motivo, aprobación no.**
5. **Irreversibilidad total.** No hay ningún método de dominio que revierta el estado tras `Approved`/`Rejected`/`Cancelled`.
6. **Qué pasa al aprobar una `InternalRequest`** (`InternalRequestApprovalReactionHandler.Handle(ApprovalCompleted)`): reconstruye directamente la secuencia de dominio equivalente:
   - `AssetAssignment` → crea `Movement` + `Assignment.Create(...)`, cambia el activo a `Reserved`.
   - `Loan` → crea `Movement` + `Loan.Create(...)`, cambia el activo a `OnLoan`.
   - `Maintenance` → abre `MaintenanceOrder` (Corrective, sin checklist), cambia el activo a `InMaintenance`.
   - En los tres casos: `request.Fulfill(fulfillmentReferenceId, ...)`. El beneficiario **siempre** es `RequestedByUserId`.
7. **Qué pasa al rechazar una `InternalRequest`**: solo `request.Reject(...)` — el activo nunca cambió de estado mientras estaba pendiente.
8. **Cancelar la `InternalRequest` no cancela la `ApprovalInstance` subyacente.** La instancia queda `Pending` indefinidamente.
9. **Notificaciones disparadas**: al crear la instancia, notifica a todos los tenedores de rol elegibles (incluso fuera de su turno en modo secuencial); al completarse/rechazarse, notifica al solicitante.
10. **Firma electrónica obligatoria para decidir.** `SignatureRecord` inmutable con hash SHA-256. No se valida que `typedFullName` coincida con el nombre real del usuario.
11. **Un flujo global (`CompanyId = null`) es un "fallback"** — prioridad al flujo específico de empresa.
12. **Un flujo nunca se edita, solo se desactiva/reemplaza.**

---

## 8. Mensajes de error/éxito — resumen consolidado

**Dominio / validación de negocio:**
- "La clave del flujo de aprobación es obligatoria."
- "Un flujo de aprobación necesita al menos un rol aprobador."
- "El número de aprobaciones requeridas debe ser al menos 1."
- "En modo secuencial se requiere una aprobación por cada rol, en orden."
- "El tipo de contexto de la aprobación es obligatorio."
- "Este flujo de aprobación exige una justificación."
- "Esta aprobación ya no está pendiente."
- "Quien solicita una aprobación no puede decidir sobre su propia solicitud."
- "Ya registraste una decisión para esta aprobación."
- "Rechazar una aprobación requiere indicar el motivo."
- "No tienes un rol elegible para decidir sobre esta aprobación."
- "Todavía no es el turno del rol correspondiente en este flujo secuencial."
- "Solo una aprobación pendiente puede cancelarse."
- "Solo quien solicitó la aprobación puede cancelarla."
- "La justificación de la solicitud es obligatoria."
- "Una solicitud de préstamo requiere la fecha esperada de devolución."
- "La fecha esperada de devolución solo aplica a solicitudes de préstamo."
- "Solo una solicitud pendiente de aprobación puede rechazarse/cancelarse/cumplirse."
- "Solo quien solicitó puede cancelar su propia solicitud."
- "Solo un activo en almacén o asignado puede reportarse a mantenimiento."
- "Solo un activo en almacén puede solicitarse."
- "No hay un flujo de aprobación configurado para '{flowKey}'. Pide a un administrador que configure uno en Aprobaciones."
- "El usuario no tiene acceso a la empresa de esta aprobación/de este activo/a la empresa indicada."
- "Se requiere iniciar sesión."
- "Uno o más roles aprobadores no existen."
- "Ya existe un flujo activo con esa clave para ese alcance. Desactívalo antes de crear uno nuevo."
- "Escribe tu nombre completo para firmar con este mecanismo."
- "Dibuja tu firma para firmar con este mecanismo."
- "Mecanismo de firma no reconocido: '{mechanism}'."

**Frontend:**
- Éxitos: "Decisión registrada."
- Errores genéricos: "No fue posible consultar los flujos de aprobación.", "No tienes permiso para consultar flujos de aprobación (Approvals.Read).", "No fue posible consultar tus aprobaciones.", "No tienes permiso para consultar solicitudes (Requests.Read).", "No fue posible consultar las solicitudes/tus solicitudes.", "Configurar un flujo requiere también poder consultar roles (Roles.Read)."
- Validación cliente: "La clave y al menos un rol aprobador son obligatorios.", "Tipo, activo y justificación son obligatorios.", "Una solicitud de préstamo requiere la fecha esperada de devolución.", "Escribe el motivo del rechazo."
- Fallbacks: "No fue posible crear el flujo de aprobación.", "No fue posible enviar la solicitud.", "No fue posible aprobar/rechazar."

---

## 9. Casos especiales / edge cases (ambigüedades señaladas explícitamente)

1. **Posible fuga entre empresas en las consultas "por id"**: `GetApprovalInstanceByIdQueryHandler` y `GetInternalRequestByIdQueryHandler` no verifican `AccessibleCompanyIds` contra la `CompanyId` de la entidad. Como esos permisos son globales, cualquier usuario con el permiso podría consultar detalles de cualquier empresa. No confirmado como diseño intencional.
2. **`cancelApprovalInstance` sin UI conectada**: existe endpoint + wrapper, pero ninguna pantalla lo invoca. Una `ApprovalInstance` huérfana de una `InternalRequest` cancelada queda visible indefinidamente en `/my-approvals`.
3. **Permisos "sembrados" sin efecto funcional**: `Approvals.Approve`, `Approvals.Reject`, `Requests.Update` no los referencia ningún comando/query.
4. **Inconsistencia entre elegibilidad de aprobador (rol) y permiso de lectura (`Approvals.Read`)**: un usuario puede decidir en `/my-approvals` sin `Approvals.Read`, y el link "Ver detalle" le daría 403.
5. **`RequiredApprovals` en modo Secuencial vs. roles duplicados**: el dominio no exige que `ApproverRoleIds` no tenga duplicados; el formulario tampoco lo impide.
6. **`GET /approval-flows` no filtra por empresa accesible al usuario que consulta** — trae todos los flujos de todas las empresas.
7. **`DecideApprovalRequest.Comment` en `Reject` puede llegar `null`**: el controller lo normaliza a `string.Empty`, y el validador FluentValidation lo rechaza con su propio mensaje (no el de dominio).
8. **`InternalRequestDetail` no expone el id de la `ApprovalInstance` asociada** — no hay navegación cruzada desde `/requests/[id]` hacia la aprobación.
9. **Reglas de elegibilidad de activo viven en el handler**, no en el dominio de `InternalRequest` ni en `AssetStateMachine`.
