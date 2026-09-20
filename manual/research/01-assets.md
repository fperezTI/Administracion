# Reporte de Análisis Exhaustivo — Módulo de Activos (Assets)

## 1. Objetivo del módulo

El módulo de Activos es el **registro maestro de inventario de TI** de la aplicación (el "Asset Registry" del dominio). Es el punto de entrada de todo el ciclo de vida de un equipo: alta con folio y etiqueta, edición de sus datos, reubicación física, y su salida definitiva del inventario (baja y disposición). Todo otro módulo del sistema (Asignaciones, Préstamos, Mantenimiento, Movimientos, Transferencias entre empresas, Aprobaciones) opera **sobre** un `Asset` ya existente: este módulo es la base de datos operativa desde la que cuelga el resto.

Lo usan usuarios internos de TI/Activos de cada empresa del grupo (la aplicación es multiempresa — `CompanyId` en cada activo). Las acciones de alta, edición, reubicación y baja/disposición requieren permisos RBAC específicos (`Assets.Create`, `Assets.Update`, `Assets.Decommission`, `Assets.Read`), validados siempre en backend, nunca solo en la UI.

Importante para el manual: **en V1 no hay depreciación ni contabilidad** — los campos financieros (costo, factura, proveedor, etc.) son puramente informativos, de captura libre, sin ningún cálculo automático.

---

## 2. Máquina de estados del Asset (`AssetStateMachine`)

Vive exclusivamente en código de dominio (`AssetManagement.Domain.Assets.AssetStateMachine`), no en base de datos ni configuración. Es un grafo dirigido fijo: cualquier intento de transición fuera de este grafo lanza una `DomainException` ("No es válido transicionar un activo de 'X' a 'Y'.") que el backend traduce a **HTTP 422 Unprocessable Entity**.

### Estados posibles (`AssetStatus`)
`InWarehouse` (En almacén) · `Reserved` (Reservado) · `Assigned` (Asignado) · `OnLoan` (Prestado) · `InTransit` (En tránsito) · `InMaintenance` (En mantenimiento) · `UnderWarranty` (En garantía) · `Damaged` (Dañado) · `Lost` (Extraviado) · `Stolen` (Robado) · `PendingDecommission` (Pendiente de baja) · `Decommissioned` (Dado de baja) · `Sold` (Vendido) · `Donated` (Donado) · `Destroyed` (Destruido).

Un activo nuevo **siempre** nace en `InWarehouse`.

### Grafo completo de transiciones

| Desde | Puede ir a |
|---|---|
| `InWarehouse` | `Reserved`, `Assigned`, `OnLoan`, `InTransit`, `InMaintenance`, `PendingDecommission` |
| `Reserved` | `InWarehouse`, `Assigned`, `OnLoan` |
| `Assigned` | `InWarehouse`, `OnLoan`, `InTransit`, `InMaintenance`, `Damaged`, `Lost`, `Stolen`, `PendingDecommission` |
| `OnLoan` | `InWarehouse`, `Assigned`, `Damaged`, `Lost`, `Stolen` |
| `InTransit` | `InWarehouse`, `Assigned` |
| `InMaintenance` | `InWarehouse`, `UnderWarranty`, `PendingDecommission`, `Damaged` |
| `UnderWarranty` | `InWarehouse`, `InMaintenance`, `PendingDecommission` |
| `Damaged` | `InMaintenance`, `PendingDecommission` |
| `Lost` | `PendingDecommission` |
| `Stolen` | `PendingDecommission` |
| `PendingDecommission` | `Decommissioned`, `InWarehouse` (retorno por rechazo de aprobación) |
| `Decommissioned` | `Sold`, `Donated`, `Destroyed` |
| `Sold` / `Donated` / `Destroyed` | (ninguna — estados terminales) |

Nota del propio código: la transición `PendingDecommission → InWarehouse` se agregó en F4 específicamente para que una solicitud de baja **rechazada** devuelva el activo a servicio; el grafo original (F2) no tenía "vuelta atrás".

### Quién/qué dispara cada transición y qué permiso exige

El `AssetStateMachine` solo valida si la transición es *estructuralmente* legal; **no** decide quién puede pedirla — eso es responsabilidad de la capa de aplicación (comandos) y del motor de Aprobaciones.

| Transición | Disparada por | Permiso RBAC | Notas |
|---|---|---|---|
| (creación) → `InWarehouse` | `CreateAssetCommand` (alta de activo) | `Assets.Create` | Único punto de creación. |
| Cualquiera de las intra-empresa que reflejan Asignación/Préstamo/Mantenimiento/Transferencia interna | Comandos de otros módulos (Inventory Operations, Maintenance) — **fuera del alcance de este módulo pero consumen `Asset.ChangeStatus`** | Permiso propio de cada módulo (p. ej. `Assignments.Create`) | El módulo de Activos no expone estos comandos directamente; se disparan desde Asignaciones/Préstamos/Mantenimiento. |
| `InWarehouse/Assigned/InMaintenance/UnderWarranty/Damaged/Lost/Stolen` → `PendingDecommission` | `RequestAssetDecommissionCommand` (endpoint `POST /assets/{id}/decommission`) | `Assets.Decommission` | Cambia el estado **inmediatamente** al pedir la baja (no espera la aprobación); además dispara una solicitud de aprobación (`asset.decommission`) vía `IApprovalCoordinator`. |
| `PendingDecommission → Decommissioned` | Reacción automática a evento `ApprovalCompleted` con `ContextType == "AssetDecommission"` (`AssetApprovalReactionHandler`) | No aplica permiso de comando — lo dispara el aprobador de la solicitud (permiso de Aprobaciones, fuera de este módulo) | Ocurre sin intervención directa del usuario del módulo Activos. |
| `PendingDecommission → InWarehouse` | Reacción automática a evento `ApprovalRejected` con `ContextType == "AssetDecommission"` | Igual que arriba | Si se rechaza la baja, el activo vuelve a `InWarehouse` automáticamente. |
| `Decommissioned → Sold/Donated/Destroyed` | Reacción automática a `ApprovalCompleted` con `ContextType` `"AssetDisposal:Sold"`/`"AssetDisposal:Donated"`/`"AssetDisposal:Destroyed"`, solicitado antes por `RequestAssetDisposalCommand` (endpoint `POST /assets/{id}/dispose`) | `Assets.Decommission` (mismo permiso que la baja, no hay uno separado para disposición) | Si se rechaza, **no pasa nada** — el activo se queda en `Decommissioned` (nunca salió de ese estado mientras la solicitud estaba pendiente). |
| `CompanyId` cambia (no es transición de `Status` propiamente) | `Asset.CompleteCrossCompanyTransfer` — exclusivo de una `Transfer` entre empresas completada (módulo de Inventario, F5) | Fuera de este módulo | Fuerza el estado a `InWarehouse` en la empresa destino y regenera el folio interno. |

**Quién configura los aprobadores**: el tipo de aprobación (`"asset.decommission"`, `"asset.disposal"`) no tiene aprobadores fijos en código — se resuelve contra un `ApprovalFlowDefinition` configurable (catálogo de Aprobaciones), es decir, es dato de configuración de la empresa, no un permiso RBAC nuevo por transición. Esto significa que el manual debe indicar que **quién aprueba una baja o disposición depende de cómo cada empresa configuró sus flujos de aprobación**, no de un permiso fijo del módulo de Activos.

---

## 3. Pantallas del módulo (frontend, `apps/web/src/app/assets`)

**Nota importante de transparencia**: a diferencia de lo indicado como convención general en `CLAUDE.md` ("contenido de UI en español vía next-intl / `es.json`"), en la práctica **todas las pantallas de Activos tienen los textos en español escritos directamente en los archivos `.tsx`** (literales embebidos), no como claves de `apps/web/messages/es.json`. Revisé `es.json` explícitamente y solo contiene 3 claves relacionadas a "assets" (`totalAssets`, `assignedAssets`, `availableAssets`), usadas en un dashboard, no en las pantallas del módulo. Por lo tanto, los textos exactos que cito abajo vienen directamente del código fuente (`.tsx`), no de `es.json` — lo señalo explícitamente porque contradice la convención documentada y conviene que quien mantenga el manual lo sepa.

### 3.1 Lista de activos (`/assets`, `page.tsx`)

Encabezado: **"Activos"** / subtítulo: **"Inventario de activos de TI por empresa."**

**Filtros (formulario GET):**
| Campo | Tipo | Obligatorio | Opciones/validación |
|---|---|---|---|
| Categoría | Select | No | "Todas" + lista de categorías activas |
| Estado | Select | No | "Todos" + los 15 estados (`ASSET_STATUS_LABELS`) |
| Buscar | Input texto | No | placeholder **"Folio, marca, modelo, serie"** — búsqueda de servidor por substring sobre `InternalFolio`, `Brand`, `Model`, `SerialNumber` |

Botón **"Filtrar"**.

**Tabla** con columnas: Folio (enlace al detalle, monoespaciado), Categoría (oculta en móvil), Marca/Modelo, Serie (oculta en móvil), Estado (badge con color semántico), Condición (oculta en móvil).

Mensaje si no hay resultados: **"No se encontraron activos con estos filtros."**

Paginación: 20 elementos por página (`PAGE_SIZE = 20`), componente `TablePagination`.

**Botones de la pantalla:**
- **"Exportar Excel"** → enlace a `/api/exports/assets?...&format=Xlsx` (respeta los filtros activos).
- **"Exportar PDF"** → enlace a `/api/exports/assets?...&format=Pdf`.
- **"Nuevo activo"** → navega a `/assets/new`.

**Manejo de errores**: si `getAssets`/`getAssetCategories` lanzan `ApiError`, se muestra:
- Si status 403: **"No tienes permiso para consultar activos (Assets.Read)."**
- Cualquier otro error: **"No fue posible consultar los activos."**

Si la empresa actual del usuario no tiene compañías asociadas (`me.companies.length === 0`), se muestra el componente `EmptyCompanyState` (no explorado en detalle, es compartido con otros módulos).

### 3.2 Alta de activo (`/assets/new`)

Encabezado: **"Nuevo activo"** / subtítulo: **"Alta y etiquetado — se genera folio y etiqueta al guardar."**

Si no hay categorías activas: **"No hay categorías de activos activas todavía. Pide a un administrador que las configure."**

**Campos del formulario (`CreateAssetForm`):**
| Campo (label exacto) | Tipo | Obligatorio | Validación cliente | Validación servidor (FluentValidation) |
|---|---|---|---|---|
| Categoría | Select | Sí (`required`) | — | `AssetCategoryId` NotEmpty |
| Marca | Input texto | Sí | `maxLength={100}` | NotEmpty, MaximumLength(100) |
| Modelo | Input texto | Sí | `maxLength={100}` | NotEmpty, MaximumLength(100) |
| Número de serie | Input texto | No (`optional`) | `maxLength={100}` | MaximumLength(100) |
| Condición física | Select, default `"Good"` (Buena) | Sí | — | (no validado explícitamente por FluentValidation, es enum) |
| Descripción | Input texto | No | `maxLength={500}` | MaximumLength(500) |
| Ubicación | Select de unidades organizacionales (árbol aplanado), default "Sin asignar" | No | — | Si se envía, debe existir y pertenecer a la misma empresa (`OrgUnit` NotFound si no) |
| Campos técnicos de {categoría} (dinámicos según `CustomFieldDefinition` de la categoría elegida) | Input texto/número/fecha, Select (Sí/No para Boolean, opciones para Select) | Depende de `field.isRequired` | `required` HTML si aplica; `type="number"`/`"date"` según tipo | El handler valida que todo campo obligatorio de la categoría esté presente y que ninguna clave enviada sea de otra categoría (si falta uno obligatorio → `ConflictException`/409; si sobra uno inválido → 409) |

Si la ubicación no tiene unidades organizacionales configuradas: **"Esta empresa todavía no tiene estructura organizacional configurada."**

**Botón**: **"Guardar y emitir etiqueta"** (texto cambia a **"Guardando…"** mientras está pendiente, `disabled`).

**Validación de servidor adicional (Server Action, antes de llamar a la API)**: si falta `companyId`, `assetCategoryId`, `brand` o `model` → **"Empresa, categoría, marca y modelo son obligatorios."**

**Error genérico**: si la API rechaza, se muestra `error.detail` de la respuesta, o **"No fue posible crear el activo."** como fallback.

**Éxito**: redirige a `/assets/{id}` (detalle del activo recién creado).

### 3.3 Detalle de activo (`/assets/[id]`)

Muestra folio (monoespaciado), marca/modelo, categoría, badge de estado, y varias tarjetas:
- **General**: Folio patrimonial, Número de serie, Condición física, Descripción.
- **Identificación** (si tiene etiqueta): Tecnología, Código (monoespaciado), Veces impresa.
- **Información financiera (informativa)** (solo si hay al menos un dato): Fecha de adquisición, Costo (+moneda), Proveedor, Factura, Orden de compra.
- **Garantía y soporte** (solo si hay al menos un dato): Inicio de garantía, Fin de garantía, Contrato de soporte, Proveedor de soporte.
- **Campos técnicos ({categoría})** (si hay valores): lista dinámica según `CustomFieldDefinition`.
- **Historial de movimientos** (si hay): Folio, Tipo, Fecha (formateada `es-MX`) — trae hasta 20 movimientos vía `getMovements`.
- Panel de **Documentos** (`DocumentsPanel`, compartido con otros módulos — adjuntos del activo).

Cualquier campo vacío se muestra como **"—"**.

Si el activo no existe (404 de la API) → página `notFound()` de Next.js.

**Botones/acciones del detalle** (todos son enlaces a otras pantallas, condicionados por el estado actual del activo):
- **"Ver etiqueta"** → siempre visible si `asset.tag` existe → `/assets/{id}/label`.
- **"Editar"** → siempre visible → `/assets/{id}/edit`.
- **"Reubicar"** → siempre visible → `/assets/{id}/relocate`.
- **"Solicitar transferencia"** → solo si `status === "InWarehouse"` → `/transfers/new?companyId=...` (módulo de Transferencias, fuera de este análisis).
- **"Abrir orden de mantenimiento"** → solo si `status` es `"InWarehouse"` o `"Assigned"` → `/maintenance-orders/new?...`.
- **"Solicitar baja"** → solo si `canRequestDecommission(status)` es verdadero (ver lista de estados elegibles abajo) → `/assets/{id}/decommission`.
- **"Solicitar disposición"** → solo si `status === "Decommissioned"` → `/assets/{id}/dispose`.

**Estados desde los que la UI permite pedir la baja** (`DECOMMISSION_ELIGIBLE_STATUSES` en `asset-labels.ts`, replicado a mano del backend — el propio comentario del código lo advierte como una duplicación manual sin paquete compartido): `InWarehouse`, `Assigned`, `InMaintenance`, `UnderWarranty`, `Damaged`, `Lost`, `Stolen`.

### 3.4 Edición de activo (`/assets/[id]/edit`)

Encabezado genérico **"Activos"**, título de página **"Editar {folio}"**, botón **"← Volver al detalle"**.

Son **tres formularios independientes** (cada uno se guarda por separado, cada uno con su propio botón "Guardar" y su propio mensaje de éxito/error):

**a) General** (`GeneralInfoForm` → `PUT /assets/{id}/general`, permiso `Assets.Update`):
| Campo | Tipo | Obligatorio | Validación |
|---|---|---|---|
| Marca | Input | Sí | `required`, `maxLength=100`; servidor: NotEmpty, MaxLength(100) |
| Modelo | Input | Sí | igual |
| Número de serie | Input | No | `maxLength=100` |
| Folio patrimonial | Input | No | `maxLength=100`; servidor MaxLength(100) |
| Condición física | Select | Sí | enum |
| Ubicación | **No editable aquí** — solo un enlace **"Reubicar activo →"** con nota: *"La ubicación se cambia desde un movimiento auditado, no desde este formulario."* |
| Descripción | Input | No | `maxLength=500` |
| Campos técnicos de {categoría} | dinámicos, iguales a alta | según `isRequired` | el handler valida que las claves pertenezcan a la categoría del activo (409 si no) |

**b) Información financiera (informativa)** (`FinancialInfoForm` → `PUT /assets/{id}/financial`, permiso `Assets.Update`):
| Campo | Tipo | Obligatorio | Validación |
|---|---|---|---|
| Fecha de adquisición | date | No | — |
| Costo | number, `step=0.01`, `min=0` | No | servidor: `GreaterThanOrEqualTo(0)` cuando no es nulo |
| Moneda (ISO 4217) | Input, `maxLength=3` | No | servidor: `Length(3)` cuando no es nulo (exactamente 3 caracteres) |
| Proveedor | Input | No | `maxLength=200` |
| Factura | Input | No | `maxLength=100` |
| Orden de compra | Input | No | `maxLength=100` |

**c) Garantía y soporte** (`ContractualInfoForm` → `PUT /assets/{id}/contractual`, permiso `Assets.Update`):
| Campo | Tipo | Obligatorio | Validación |
|---|---|---|---|
| Inicio de garantía | date | No | — |
| Fin de garantía | date | No | — |
| Contrato de soporte | Input | No | `maxLength=100` |
| Proveedor de soporte | Input | No | `maxLength=200` |

Regla cruzada servidor: **"La fecha de inicio de garantía debe ser anterior o igual a la fecha de fin."** (solo se valida si ambas fechas están presentes).

**Mensajes comunes a los tres formularios** (`SaveBar`): éxito → **"Guardado."** (verde); error → `error.detail` de la API o **"No fue posible guardar los cambios."**; botón dice **"Guardando…"** mientras está pendiente.

### 3.5 Reubicación (`/assets/[id]/relocate`)

Encabezado **"Reubicar activo"**, subtítulo `"{folio} — {marca} {modelo}"`. Botón **"← Volver"**.

**Campos:**
| Campo | Tipo | Obligatorio | Validación |
|---|---|---|---|
| Nueva ubicación | Select (árbol de unidades organizacionales aplanado), default = ubicación actual, opción **"Sin asignar"** | No | servidor: si se indica, debe existir en la misma empresa del activo |
| Motivo (opcional) | Input, `maxLength=500` | No | servidor: `MaximumLength(500)` |

Nota si no hay unidades organizacionales: **"Esta empresa todavía no tiene estructura organizacional configurada."**

**Botón**: **"Reubicar"** (→ **"Guardando…"** mientras pendiente).

**Regla de negocio del servidor**: si la nueva ubicación es igual a la actual → **409 Conflict**: **"El activo ya está en esa unidad organizacional."**

**Éxito**: redirige a `/assets/{id}`. Esta operación **no cambia `Status`**, solo `CurrentOrgUnitId`, y genera un `Movement` de tipo `Relocation` con folio `MOV-REL-NNNNNN`, auditado (`IAuditableCommand`).

### 3.6 Solicitar baja / decommission (`/assets/[id]/decommission`)

Encabezado **"Solicitar baja"**, subtítulo `"{folio} — {marca} {modelo}"`. Botón **"← Volver"**.

**Campo único:**
| Campo | Tipo | Obligatorio | Validación |
|---|---|---|---|
| Justificación | `textarea`, `rows=4`, `maxLength=1000`, placeholder: **"Motivo de la baja — se envía como evidencia junto con la solicitud de aprobación."** | Sí | cliente: `required`; server action: si viene vacía tras `trim()` → **"La justificación es obligatoria."**; FluentValidation: `NotEmpty().MaximumLength(1000)` |

**Botón**: **"Solicitar baja"** (→ **"Enviando…"**).

**Éxito**: redirige a `/assets/{id}` (**no** a la pantalla de la solicitud de aprobación — el propio código comenta que el solicitante puede no tener el permiso `Approvals.Read`, así que ver el activo en "Pendiente de baja" es la confirmación).

**Efecto inmediato en backend**: el estado del activo cambia a `PendingDecommission` **de inmediato** (no espera la aprobación) y se crea la instancia de aprobación tipo `"asset.decommission"`.

### 3.7 Solicitar disposición (`/assets/[id]/dispose`)

Encabezado **"Solicitar disposición"**, subtítulo `"{folio} — {marca} {modelo}"`. Botón **"← Volver"**.

**Campos:**
| Campo | Tipo | Obligatorio | Validación |
|---|---|---|---|
| Destino | Select, default **"Venta"** (`Sold`) | Sí | Opciones: **Venta** (`Sold`), **Donación** (`Donated`), **Destrucción** (`Destroyed`). Servidor: mensaje **"El destino de disposición debe ser Sold, Donated o Destroyed."** (queda en inglés/nombres de enum, no traducido — inconsistencia de UX a señalar) |
| Justificación | `textarea`, `rows=4`, `maxLength=1000` (sin placeholder) | Sí | igual que en baja: NotEmpty, MaximumLength(1000) |

**Botón**: **"Solicitar disposición"** (→ **"Enviando…"**).

**Regla de negocio del servidor**: solo se puede pedir disposición si el activo está actualmente en `Decommissioned` — si no, **409 Conflict**: **"Solo un activo dado de baja puede solicitarse para disposición."**

**Éxito**: redirige a `/assets/{id}`. A diferencia de la baja, **no hay un estado "pendiente de disposición"** — el activo permanece visualmente en `Decommissioned` mientras la solicitud está pendiente.

### 3.8 Etiqueta (`/assets/[id]/label`)

Sin `AppHeader` (pantalla pensada para imprimirse, `print:p-0`, `print:hidden` en los controles).

Si el activo no tiene etiqueta emitida (`!asset.tag`) → `notFound()`.

**Contenido de la etiqueta:**
- Nombre comercial de la empresa (`company.tradeName`, o **"Empresa"** si no cargó).
- Nombre de la categoría (o **"Categoría"**).
- **Código QR** (imagen `data:` generada con la librería `qrcode`) — solo si `technology` es `Qr` o `QrAndBarcode`. Para `Barcode`/`Nfc`/`Rfid` puros, en vez de imagen se muestra el texto: **"Tecnología {tecnología} — se codifica con el equipo de impresión/grabado correspondiente."**
- Folio interno en grande.
- Código de la etiqueta (monoespaciado, es un GUID sin guiones).
- Pie de página (oculto al imprimir): **"Impresa {n} {vez|veces}. Reimprimir no cambia la identidad del activo."**

**Botones:**
- **"← Volver al activo"**.
- **"Reimprimir (+1)"** → llama a `POST /assets/{id}/tag/reprint`, incrementa `PrintCount`, permiso `Assets.Update`. Revalida la página tras la acción.
- Botón de impresión (`PrintButton`, componente compartido — dispara `window.print()`).

---

## 4. Resumen de todos los botones/acciones por pantalla

| Pantalla | Botón/acción | Efecto |
|---|---|---|
| Lista | Filtrar | Recarga la lista con los filtros (GET, query string) |
| Lista | Exportar Excel / Exportar PDF | Descarga vía `GET /api/exports/assets` (respeta filtros) |
| Lista | Nuevo activo | Navega a `/assets/new` |
| Alta | Guardar y emitir etiqueta | `POST /assets` — crea el activo, genera folio, emite etiqueta en una sola operación |
| Detalle | Ver etiqueta | Navega a `/assets/{id}/label` |
| Detalle | Editar | Navega a `/assets/{id}/edit` |
| Detalle | Reubicar | Navega a `/assets/{id}/relocate` |
| Detalle | Solicitar transferencia | Navega a `/transfers/new` (otro módulo) |
| Detalle | Abrir orden de mantenimiento | Navega a `/maintenance-orders/new` (otro módulo) |
| Detalle | Solicitar baja | Navega a `/assets/{id}/decommission` |
| Detalle | Solicitar disposición | Navega a `/assets/{id}/dispose` |
| Editar → General | Guardar | `PUT /assets/{id}/general` |
| Editar → Financiera | Guardar | `PUT /assets/{id}/financial` |
| Editar → Garantía/soporte | Guardar | `PUT /assets/{id}/contractual` |
| Reubicar | Reubicar | `POST /assets/{id}/relocate` — crea `Movement` auditado, cambia `CurrentOrgUnitId` |
| Baja | Solicitar baja | `POST /assets/{id}/decommission` — cambia `Status` a `PendingDecommission` + crea solicitud de aprobación |
| Disposición | Solicitar disposición | `POST /assets/{id}/dispose` — crea solicitud de aprobación (sin cambiar `Status` de inmediato) |
| Etiqueta | Reimprimir (+1) | `POST /assets/{id}/tag/reprint` — incrementa contador de impresión |
| Etiqueta | Imprimir | Solo cliente, `window.print()`, no llama API |

---

## 5. Endpoints REST (`AssetsController`, ruta base `api/v{version}/assets`, todos requieren `[Authorize]` — sesión Entra ID válida)

Ninguna acción tiene `[Authorize(Policy=...)]` explícito a nivel de controlador/acción: la autorización por permiso (`Assets.Read/Create/Update/Decommission`) se hace **enteramente vía MediatR** (`AuthorizationBehavior`, capa Application), no vía atributos ASP.NET — es "defensa en profundidad": revalida el permiso del `IRequiresPermission` del comando/consulta sin importar quién lo invoque.

Mapeo global de excepciones a HTTP (`GlobalExceptionHandler`, aplica a **todos** los endpoints):
- `ValidationException` (FluentValidation) → **400 Bad Request** (con detalle de errores por campo en `problemDetails.extensions.errors`)
- `ForbiddenAccessException` → **403 Forbidden**
- `NotFoundException` → **404 Not Found**
- `ConflictException` → **409 Conflict**
- `DbUpdateConcurrencyException` (choque de concurrencia optimista, hay `RowVersion` en `Assets`) → **409 Conflict**, mensaje: *"The record was modified by someone else. Reload and try again."*
- `DomainException` (violación de regla de dominio, p. ej. transición de estado inválida) → **422 Unprocessable Entity**
- Cualquier otra excepción → **500** genérico (sin detalle interno, solo `correlationId`)

| Método y ruta | Query/Body | Respuestas | Permiso (`IRequiresPermission`) |
|---|---|---|---|
| `GET /assets` | Query: `companyId` (requerido), `pageNumber=1`, `pageSize=50`, `assetCategoryId?`, `status?`, `search?` | 200 `PagedResult<AssetSummary>`; 403 si no tiene acceso a esa empresa | `Assets.Read` |
| `GET /assets/{assetId}` | — | 200 `AssetDetail`; 404 si no existe | `Assets.Read` |
| `POST /assets` | `CreateAssetCommand` completo (ver §3.2) | 201 Created (`CreateAssetResult`: `AssetId`, `InternalFolio`, `TagCode`), header `Location`; 400 validación; 403 sin acceso a empresa/permiso; 404 categoría u OrgUnit inexistente; 409 categoría desactivada o campos personalizados inválidos/faltantes | `Assets.Create` |
| `PUT /assets/{assetId}/general` | `UpdateAssetGeneralInfoRequest` | 204 No Content; 400; 404 activo no existe; 409 campos personalizados inválidos | `Assets.Update` |
| `PUT /assets/{assetId}/financial` | `UpdateAssetFinancialInfoRequest` | 204; 400; 404 | `Assets.Update` |
| `PUT /assets/{assetId}/contractual` | `UpdateAssetContractualInfoRequest` | 204; 400 (incluye regla cruzada de fechas); 404 | `Assets.Update` |
| `POST /assets/{assetId}/tag/reprint` | — | 200 `ReprintAssetTagResult` (`Code`, `PrintCount`); 404 activo o etiqueta inexistente (422 si no tiene etiqueta emitida) | `Assets.Update` |
| `POST /assets/{assetId}/relocate` | `RelocateAssetRequest` (`NewOrgUnitId?`, `Notes?`) | 200 `RelocateAssetResult` (`MovementId`, `MovementFolio`); 400; 403 sin acceso a la empresa del activo; 404 activo u OrgUnit inexistente; 409 si ya está en esa ubicación | `Assets.Update` |
| `POST /assets/{assetId}/decommission` | `RequestAssetDecommissionRequest` (`Justification`) | 200 `Guid` (id de la instancia de aprobación); 400 justificación vacía/larga; 403 sin acceso a la empresa; 404 activo inexistente; 422 si el estado actual no admite pasar a `PendingDecommission` | `Assets.Decommission` |
| `POST /assets/{assetId}/dispose` | `RequestAssetDisposalRequest` (`TargetStatus`, `Justification`) | 200 `Guid` (id de la instancia de aprobación); 400 destino inválido o justificación vacía; 403; 404; 409 si el activo no está en `Decommissioned` | `Assets.Decommission` |

Nota: los botones "Exportar Excel/PDF" del listado llaman a `GET /api/exports/assets`, que pertenece a `ExportsController` (documentado en el módulo de Import/Export/Reportes).

---

## 6. Reglas de negocio no obvias encontradas en el dominio

1. **`CompanyId` nunca se edita directo** (`Asset.CompanyId` tiene setter privado). El único método que lo cambia es `Asset.CompleteCrossCompanyTransfer(...)`, invocado exclusivamente por una `Transfer` entre empresas ya completada. El `InternalFolio` es único solo por empresa (índice `(CompanyId, InternalFolio)` único), así que al cambiar de empresa se **regenera un folio nuevo** en la secuencia de la empresa destino. Esa transferencia además **limpia `CurrentOrgUnitId`** y fuerza el estado a `InWarehouse`.

2. **El código de la etiqueta (`AssetTag.Code`) es independiente del folio interno**, deliberadamente. El folio (`ASSET-000001`) es único solo dentro de la empresa; el código de la etiqueta es un GUID sin guiones, único globalmente (índice único en `AssetTags.Code`), porque un QR/código de barras debe resolverse sin ambigüedad sin importar la empresa del lector.

3. **Reimprimir una etiqueta nunca reemite un código nuevo** — solo incrementa `PrintCount` y actualiza `LastPrintedAtUtc`. `AssetTag.Code` es inmutable desde su emisión.

4. **Un activo solo puede tener una etiqueta emitida en toda su vida** (`Asset.IssueTag` lanza `DomainException` — *"Este activo ya tiene una etiqueta emitida."*). Como la etiqueta se emite automáticamente al crear el activo, **no existe un flujo de re-emisión de etiqueta**, solo reimpresión del mismo código.

5. **Reubicación reemplazó una edición silenciosa de campo.** Antes la ubicación se editaba como cualquier otro campo del formulario general; ahora se separó en su propio comando porque cada reubicación debe dejar un `Movement` auditado con folio propio (`MOV-REL-NNNNNN`). Por eso el formulario de edición general deshabilita el campo de ubicación y solo enlaza a la pantalla de reubicación.

6. **La solicitud de baja mueve el estado de inmediato**, antes de que exista una decisión de aprobación.

7. **La solicitud de disposición NO mueve el estado mientras está pendiente** — `Decommissioned` ya es un estado de "reposo" válido. El tipo de disposición deseado se codifica dentro del `ContextType` de la aprobación (`"AssetDisposal:Sold"`, etc.).

8. **La "evidencia" de la baja/disposición es solo texto de justificación**, no un archivo adjunto real — marcado en el código como simplificación deliberada.

9. **Categorías inactivas no admiten altas nuevas.** `CreateAssetCommand` valida `category.IsActive` y lanza 409 si está desactivada, aunque activos existentes con esa categoría sigan intactos.

10. **Campos personalizados por categoría son dinámicos y con obligatoriedad configurable** (`CustomFieldDefinition.IsRequired`). El backend valida server-side que todo campo obligatorio esté presente y que ninguna clave enviada corresponda a otra categoría. Un campo tipo `Select` debe definir sus `Options` al crearse.

11. **`Currency` se normaliza a mayúsculas** al guardar, aunque el formulario no lo indique visualmente.

12. **Concurrencia optimista real** (`RowVersion` en `Assets`) — dos ediciones simultáneas producen 409 en la segunda, con mensaje genérico.

---

## 7. Mensajes de error/éxito relevantes (texto literal encontrado en el código)

**Del dominio (`DomainException` → 422):**
- "El folio interno es obligatorio."
- "La marca del activo es obligatoria."
- "El modelo del activo es obligatorio."
- "El nuevo folio interno es obligatorio." (transferencia entre empresas)
- "Este activo ya tiene una etiqueta emitida."
- "Este activo todavía no tiene una etiqueta emitida."
- "No es válido transicionar un activo de '{X}' a '{Y}'."
- "El nombre de la categoría es obligatorio." / "El código de la categoría es obligatorio."
- "Ya existe un campo personalizado con el código '{code}' en esta categoría."
- "Un campo de selección debe definir sus opciones."
- "El código de la etiqueta es obligatorio."

**De la capa de aplicación (`ConflictException` → 409):**
- "El usuario no tiene acceso a la empresa indicada." / "...a la empresa de este activo."
- "La categoría indicada está desactivada."
- "Faltan campos obligatorios de la categoría: {lista de nombres}."
- "Uno o más campos personalizados no pertenecen a esta categoría."
- "El activo ya está en esa unidad organizacional."
- "Solo un activo dado de baja puede solicitarse para disposición."
- "Ya existe una categoría con el código '{code}'." (alta de categoría)

**FluentValidation:**
- "La fecha de inicio de garantía debe ser anterior o igual a la fecha de fin."
- "El destino de disposición debe ser Sold, Donated o Destroyed." — **queda sin traducir** (inconsistencia de UX a señalar en el manual).

**Del frontend (server actions / componentes):**
- "Empresa, categoría, marca y modelo son obligatorios." (alta)
- "No fue posible crear el activo." (fallback alta)
- "No fue posible guardar los cambios." (fallback edición, los 3 formularios)
- "Guardado." (éxito edición)
- "No fue posible reubicar el activo." (fallback reubicación)
- "La justificación es obligatoria." (baja y disposición, validación cliente)
- "No fue posible solicitar la baja." / "No fue posible solicitar la disposición." (fallback)
- "No tienes permiso para consultar activos (Assets.Read)." (403 en el listado)
- "No fue posible consultar los activos." (otro error en el listado)
- "No se encontraron activos con estos filtros." (lista vacía)
- "No hay categorías de activos activas todavía. Pide a un administrador que las configure." (alta sin categorías)
- "Esta empresa todavía no tiene estructura organizacional configurada." (alta y reubicación, sin OrgUnits)
- "La ubicación se cambia desde un movimiento auditado, no desde este formulario." (nota en edición general)
- "Impresa {n} {vez|veces}. Reimprimir no cambia la identidad del activo." (etiqueta)

El frontend prioriza mostrar el `detail` exacto del servidor sobre el mensaje genérico local cuando existe.

---

## 8. Casos especiales / edge cases detectados en el código

1. **Posible brecha de validación de empresa** en `GetAssetByIdQueryHandler`, `ReprintAssetTagCommandHandler`, `UpdateAssetGeneralInfoCommandHandler`, `UpdateAssetFinancialInfoCommandHandler` y `UpdateAssetContractualInfoCommandHandler`: no validan `currentCompany.AccessibleCompanyIds` contra `asset.CompanyId`, a diferencia de `GetAssetsQuery`, `CreateAssetCommand`, `RelocateAssetCommand`, `RequestAssetDecommissionCommand` y `RequestAssetDisposalCommand`, que sí lo hacen. Dado que RBAC aquí es por permiso global (no acotado por empresa), esto podría implicar que cualquier usuario con `Assets.Read`/`Update` acceda a un activo de una empresa a la que no pertenece, conociendo el GUID. Señalado como hallazgo de código, no como interpretación — se recomienda confirmar con el equipo si es deliberado.

2. **Rechazo de baja regresa siempre a `InWarehouse`**, sin importar el estado previo a la solicitud.

3. **Rechazo de disposición es un no-operación total** — sin mensaje ni cambio de estado visible en este módulo.

4. El botón "Solicitar baja" desaparece de la UI una vez en `PendingDecommission` (evita reintentos desde la pantalla); a nivel de comando, un segundo intento fallaría con 422 por la máquina de estados.

5. **Auditoría desigual**: solo `CreateAssetCommand`, `RelocateAssetCommand`, `RequestAssetDecommissionCommand` y `RequestAssetDisposalCommand` implementan `IAuditableCommand`. Editar información general/financiera/contractual o reimprimir etiqueta **no** genera entrada en el panel de auditoría funcional — solo logs técnicos de Serilog sin PII.

6. Búsqueda de texto en la lista usa `Contains` de EF Core (`LIKE '%term%'`); la sensibilidad a mayúsculas depende de la collation de SQL Server, no está normalizada en la consulta.

7. `SerialNumber` es único por empresa solo cuando no es nulo (índice filtrado). Una violación produciría probablemente un 500 genérico, ya que `GlobalExceptionHandler` no maneja explícitamente `DbUpdateException` por índice único (solo `DbUpdateConcurrencyException`).

8. La etiqueta con tecnología `Barcode`, `Nfc` o `Rfid` pura no tiene previsualización visual — solo texto explicando que requiere hardware especializado.

9. El formulario de disposición siempre ofrece las tres opciones sin filtrar según el estado real del activo; la validación de que esté en `Decommissioned` ocurre solo en el servidor.

10. No hay confirmación intermedia ("¿está seguro?") en ninguna acción irreversible (baja, disposición, reubicación) — el envío es directo; la única protección es la aprobación posterior de otra persona.

---

## Archivos revisados

**Domain**: `Asset.cs`, `AssetStateMachine.cs`, `AssetStatus.cs`, `AssetTag.cs`, `AssetCategory.cs`, `AssetCustomFieldValue.cs`, `CustomFieldDataType.cs`, `CustomFieldDefinition.cs`, `IdentificationTechnology.cs`, `PhysicalCondition.cs` (todos en `apps/api/src/AssetManagement.Domain/Assets/`).

**Application**: `CreateAssetCommand.cs`, `GetAssetByIdQuery.cs`, `GetAssetsQuery.cs`, `ReprintAssetTagCommand.cs`, `UpdateAssetContractualInfoCommand.cs`, `UpdateAssetFinancialInfoCommand.cs`, `UpdateAssetGeneralInfoCommand.cs`; `Categories/CreateAssetCategoryCommand.cs`, `Categories/AddCustomFieldDefinitionCommand.cs`, `Categories/SetAssetCategoryActiveCommand.cs`; `Inventory/RelocateAssetCommand.cs`, `Inventory/RequestAssetDecommissionCommand.cs`, `Inventory/RequestAssetDisposalCommand.cs`, `Inventory/AssetApprovalReactionHandler.cs`; `Common/Security/PermissionCatalog.cs`, `Common/Security/IAuditableCommand.cs`, `Common/Security/FolioDocumentTypes.cs`; `Common/Behaviors/AuthorizationBehavior.cs`, `Common/Behaviors/ValidationBehavior.cs`, `Common/Behaviors/AuditBehavior.cs`.

**Api**: `Controllers/V1/AssetsController.cs`; `Middleware/GlobalExceptionHandler.cs`.

**Infrastructure**: `Persistence/Configurations/Assets/AssetConfiguration.cs`, `AssetCategoryConfiguration.cs`, `AssetTagConfiguration.cs`, `AssetCustomFieldValueConfiguration.cs`; `Persistence/EfFolioGenerator.cs`.

**Web**: todo `apps/web/src/app/assets/**`; `apps/web/src/lib/asset-labels.ts`; `apps/web/messages/es.json` (verificado, casi sin uso en este módulo).
