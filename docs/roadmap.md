# Roadmap

## V1 (alcance de este proyecto) — ver backlog detallado en `docs/architecture/00-analysis.md` §13

F0 Fundacional ✅ → F1 Identity & Organization ✅ → F2 Catálogos + Asset Registry ✅ → F3 Inventory
Operations ✅ → F4 Approvals + E-Signature ✅ → F5 Transferencias entre empresas ✅ → F6 Mantenimientos +
Refacciones/Consumibles ✅ → F7 Solicitudes internas ✅ → F8 Documentos/Notificaciones/Auditoría UI ✅ →
F9 Importaciones/Exportaciones ✅ → F10 Reportes y paneles ✅ → F11 Búsqueda avanzada ✅ →
F12 Privacidad/retención + hardening + carga ✅ → F13 Azure/CI-CD productivo ✅ (infraestructura y CI/CD
listos; sin despliegue real — ver "Estado de F13").

**El roadmap de V1 está completo.** F0, F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11 y F12 tienen backend
**y** frontend completos (no solo API) y un sistema real corriendo verificado en cada fase — ver el
detalle de cada uno abajo. F13 es la excepción: entrega la infraestructura como código y los flujos de
CI/CD completos, pero no un despliegue real (ver "Estado de F13" para por qué).

## Estado de F1 (Identity & Organization)

**Backend** (con pruebas): autenticación Entra ID vía `Microsoft.Identity.Web`; aprovisionamiento de perfil
local en primer login; `Company` (alta/lectura/activación, tope de 50); `OrgUnit` jerárquico con 11 tipos
sembrados (alta, mover con detección de ciclos, activación); RBAC completo (68 permisos sembrados, roles con
alta/edición/duplicado/activación, matriz rol↔permiso, `AuthorizationBehavior` exigiendo el permiso
declarado por cada comando/consulta); resolución de empresa activa vía encabezado validado contra membresía
real; filtro de consulta global multiempresa sobre `OrgUnit`. 76 pruebas acumuladas con F2 (65 unitarias, 11
de integración con SQL Server real vía Testcontainers y un `TestAuthHandler`).

**Frontend**: login real con NextAuth (Auth.js) v5 + proveedor Entra ID (Authorization Code + PKCE, patrón
BFF — el access token nunca llega al navegador), **logout federado** (cierra también la sesión en Entra ID,
no solo la cookie local), manejo explícito de un *refresh token* inválido (`session.error ===
"RefreshAccessTokenError"` fuerza replay del login en vez de arrastrar una sesión rota). Login de punta a
punta verificado con un usuario real del tenant (ver `docs/security/authentication.md`, sección
"Verificación realizada"; los dos ajustes de Entra ID que hicieron falta quedaron documentados en
`docs/security/entra-id-setup.md`).

**UI de administración** (nueva en este incremento): `/companies` (listar, crear, activar/desactivar);
`/org-units` (árbol por empresa, crear, mover con selector de nueva unidad padre, activar/desactivar);
`/roles` (listar, crear, y en el detalle: renombrar, duplicar, activar/desactivar, y el **editor de matriz
de permisos** — casillas agrupadas por módulo, guardadas con `PUT /roles/{id}/permissions`); `/users`
(listar, y en el detalle: asignar/quitar roles, otorgar/revocar acceso a empresas). Un `AppHeader`
compartido da navegación consistente entre todas las pantallas y el logout federado.

**Pendiente** (no bloqueante): la matriz de permisos no distingue visualmente permisos de módulos que aún
no tienen ninguna pantalla propia (p. ej. `Maintenance`, `Transfers` — existen en el catálogo desde F1 pero
sin UI que los consuma todavía, ver `docs/security/authorization-rbac.md`; `Assignments`/`Loans`/`Movements`
ya tienen UI propia desde F3); el formulario "mover" de
`OrgUnit` no valida en el cliente que la nueva unidad padre no sea descendiente de la que se mueve (el
backend sí lo rechaza — `MoveOrgUnitCommandHandler` — pero el error se muestra como página de error genérica
de Next.js en vez de un mensaje en el formulario, porque esa acción no pasa por `useActionState`).

## Estado de F2 (Catálogos + Asset Registry)

**Backend** (con pruebas): `AssetCategory` con campos personalizados configurables por categoría
(`CustomFieldDefinition`, con validación de obligatoriedad al dar de alta un activo) y 9 categorías
iniciales sembradas (pedido §10); `Asset` con datos generales/financieros informativos/operativos/
contractuales (pedido §11), concurrencia optimista (`RowVersion`) y auditoría de creación/modificación
(`AuditableAggregateRoot`); máquina de estados completa (`AssetStateMachine`, 15 estados, grafo probado) —
solo `InWarehouse` es alcanzable hasta que F3/F4/F6 conecten sus transiciones; identificación física
(`AssetTag`: emisión, reimpresión sin cambiar identidad, código globalmente único — ver ADR 0004);
generación atómica de folios por empresa (`IFolioGenerator`/`EfFolioGenerator`, formato `ASSET-000001`).
Flujo E2E obligatorio "alta y etiquetado de un activo" (pedido §35) cubierto y verificado contra SQL Server
real.

**Bug real encontrado y corregido durante la verificación**: el código de etiqueta reutilizaba el folio
(único solo por empresa) como identificador único global, causando colisiones entre empresas — ver ADR 0004.

**Frontend**: `/assets` (listado paginado con filtros por categoría/estado/búsqueda y selector de empresa),
`/assets/new` (alta con campos técnicos dinámicos según la categoría elegida y **selector de ubicación**
poblado desde la estructura organizacional real de la empresa), `/assets/[id]` (detalle), `/assets/[id]/edit`
(edición de general/financiero/contractual — tres formularios independientes, uno por comando del backend),
`/assets/[id]/label` (**vista previa de etiqueta con código QR generado del lado del servidor**, botón de
reimprimir e "Imprimir" con `window.print()`), `/asset-categories` (listar), `/asset-categories/new` (crear
categoría), `/asset-categories/[id]` (detalle, agregar campos técnicos, activar/desactivar). Probado
manualmente contra el stack real de Docker Compose, incluyendo datos sembrados directamente en la base para
la cuenta real del tenant.

**Pendiente** (no bloqueante): la vista de etiqueta solo renderiza QR como imagen — tecnologías
Barcode/NFC/RFID muestran el código como texto con una nota, porque su codificación física requiere hardware
especializado de impresión/grabado, no solo una imagen en el navegador (documentado en el código); validación
de tipo de dato de los campos personalizados más allá de "obligatorio" (Number/Date/Boolean/Select se
capturan con el `<input>` nativo correspondiente pero sin validación cruzada adicional).

## Estado de F3 (Inventory Operations)

**Ambigüedad de dependencia resuelta**: el roadmap listaba "F2, motor de firma" como dependencia de F3, pero
el motor de firma configurable es un entregable de F4. F3 construye su propia firma electrónica simple
(un solo mecanismo) para cumplir el invariante ya fijado en `domain-model.md` desde el análisis inicial —
ver ADR 0005 para el razonamiento completo.

**Backend** (con pruebas): `Movement` (agregado inmutable una vez `Completed`, folio propio por empresa y
tipo — `Assignment`, `AssignmentReturn`, `Loan`, `LoanReturn`, `Relocation`); `Assignment` de dos pasos
(`Create` → `Reserved` + `PendingSignature`; `Accept` autoservicio del destinatario → `Assigned`; devolución
también firmada, por quien la ejecuta); `Loan` de un paso, sin firma (préstamo informal de corto plazo, a
diferencia de la asignación formal); `SignatureRecord` (firma electrónica simple — nombre escrito + usuario
autenticado real + IP/user-agent + hash SHA-256 del contenido, ver ADR 0005); `Asset.ChangeStatus` como
único punto de entrada al `AssetStateMachine` (antes existía el grafo pero ningún comando lo usaba);
reubicación intra-empresa auditada (`RelocateAssetCommand`, reemplaza la edición libre y no auditada de
`CurrentOrgUnitId` que tenía el formulario general de F2). 110 pruebas acumuladas (96 unitarias, 14 de
integración con SQL Server real, incluyendo el flujo E2E completo "asignar → firmar → devolver" con dos
usuarios autenticados distintos).

**Bug real encontrado y corregido durante la verificación**: `GetMyAssignmentsQuery` ordenaba por
`a.Status == AssignmentStatus.PendingSignature descending` — el proveedor de SQL Server traduce esa
comparación booleana sobre un enum mapeado a `nvarchar` a un operador `^` inválido entre `nvarchar`
(`Cannot... nvarchar and nvarchar are incompatible in the '^' operator`), un error que el proveedor
InMemory usado en pruebas unitarias no reproduce — solo apareció al probar `/my-assignments` con datos
reales. Se corrigió con una clave de orden explícita (`? 0 : 1` en vez de la comparación booleana directa)
y se agregó cobertura de integración para `/assignments/mine` que reproduce el caso.

**Frontend**: `/assignments` (listado admin, alta), `/assignments/[id]` (detalle, firma visible, cancelar
si pendiente, devolver si aceptada), `/my-assignments` (**autoservicio** — el único enlace de navegación sin
permiso RBAC, disponible para cualquier usuario autenticado: confirma la recepción de lo pendiente con un
campo de texto como firma simple); `/loans`, `/loans/new`, `/loans/[id]` (devolución); `/movements`
(historial paginado con filtro por tipo); sección "Historial de movimientos" en el detalle de cada activo;
`/assets/[id]/relocate` (reemplaza el campo "Ubicación" que tenía el formulario de edición general).
Verificado con Playwright y una sesión real (mismo mecanismo del rediseño de UI): navegación, estados
vacíos y manejo de 403 confirmados con la cuenta real del tenant — la cuenta de prueba disponible no tiene
los permisos `Assignments.*`/`Loans.*`/`Movements.Read` todavía (se crearon en F1 pero ningún rol los tenía
asignados hasta ahora), así que los flujos completos de alta/firma/devolución quedaron verificados por las
pruebas de integración E2E, no visualmente con esa cuenta — pendiente que el usuario se otorgue esos
permisos desde `/roles` si quiere probarlos de punta a punta en el navegador.

**Pendiente** (no bloqueante, documentado en ADR 0005): F4 ya agregó un segundo mecanismo de firma
(`DrawnSignature`), pero los flujos de F3 (`SignAssignmentCommand`/`ReturnAssignmentCommand`) siguen
usando solo `TypedConfirmation` — no se retocaron para ofrecer la firma dibujada, deliberado para no tocar
código ya probado sin una razón funcional; sin flujo de autoservicio para la firma de devolución (la
ejecuta quien recibe físicamente el activo de vuelta, en un solo paso); reubicación sin restricción de
estado del activo (se
puede reubicar un activo en cualquier estado, no solo `InWarehouse` — deliberado, para poder corregir
ubicación sin importar el estado operativo); el selector de destinatario en alta de asignación/préstamo
lista todos los usuarios del sistema sin filtrar por membresía de empresa (el backend sí valida la
membresía y rechaza si no aplica — la lista solo podría mostrar opciones que luego se rechazan).

**Seguimiento (reasignación + resguardo imprimible)**: la ficha de un activo (`/assets/[id]`) no enlazaba
al flujo de asignación existente, y "reasignar" (devolver + asignar a otra persona en un solo paso) nunca
se construyó — quedó documentado como scope de F3 pero nunca implementado. Se agregó `ReassignAssetCommand`
(nuevo permiso `Assignments.Reassign`), que compone `Return` + `Create` atómicamente encadenando las
transiciones `Assigned → InWarehouse → Reserved` (ambas ya válidas en `AssetStateMachine`, sin tocar el
grafo); botones "Asignar"/"Reasignar" en la ficha del activo; y una página imprimible de resguardo
(`/assignments/[id]/resguardo`, mismo patrón HTML + `window.print()` que la etiqueta QR — no PDF de
servidor) como comprobante para el expediente físico, sin reemplazar la firma digital ya existente
(`SignAssignmentCommand`). La cláusula de responsabilidad del resguardo es un placeholder razonable escrito
directamente en la página (no se conectó al catálogo `Templates`, que todavía no tiene motor de
renderizado) — pendiente que legal/RH del cliente la revise y ajuste.

**Seguimiento (activos accesorios — paquetes de asignación)**: pedido del cliente para poder vincular
activos secundarios a uno principal (ejemplo dado: una laptop y su cargador) de forma que se asignen,
firmen, devuelvan y reasignen siempre juntos. No existía ningún concepto de relación activo↔activo antes
de esto. Se agregó `Asset.AccessoryOfAssetId` (FK opcional auto-referenciada, restringida a un solo nivel
— un accesorio no puede tener accesorios propios) gestionable desde la ficha del activo principal
(`/assets/[id]/link-accessory`); y `Assignment.AssignmentGroupId` (nullable, sin backfill — una asignación
sin accesorios sigue sin grupo, comportamiento idéntico al de antes de esta feature). Toda la cascada
(crear, firmar, devolver, cancelar, reasignar) vive en un único helper compartido
(`AssignmentGroupSupport.cs`) para no duplicar la lógica en los cinco comandos que la usan. El formulario
de asignación y el de reasignación muestran los accesorios disponibles del activo principal como casillas
premarcadas (se pueden excluir por asignación); "Mis asignaciones" agrupa las tarjetas por paquete con una
sola confirmación de firma; el resguardo imprimible lista todos los activos del paquete cuando aplica.

## Estado de F4 (Approvals + E-Signature — núcleo)

**Ambigüedades de diseño resueltas**: el boceto original del motor de aprobaciones (`domain-model.md`,
escrito en el análisis inicial) incluía `Threshold: Unanimous | Minimum(n)` y listas de personas fijas —
al construirlo se descubrió que con aprobadores basados en roles (dinámicos, como el resto del RBAC del
sistema) "unánime" no está bien definido y "secuencial" solo tiene sentido como un orden entre **roles**,
no entre personas. Ver ADR 0006 para el razonamiento completo y por qué `RequiredApprovals: int` (el
"N" de "N-de-M" que pide C4) es la única pieza que sobrevivió del boceto original.

**Backend** (con pruebas): `ApprovalFlowDefinition`/`ApprovalInstance`/`ApprovalStep` (motor genérico,
contexto Approvals — nunca importa nada de otro contexto, reacciona vía eventos de dominio);
`IApprovalCoordinator` (puerto que cualquier módulo usa para pedir una aprobación sin saber cómo funciona
el motor); **primer despacho real de eventos de dominio del sistema** — `AggregateRoot.Raise`/
`DomainEvents` existían desde F0 sin usarse, `AppDbContext.SaveChangesAsync` ahora los recolecta y publica
(vía MediatR `IPublisher`, con un envoltorio `DomainEventNotification<T>` para que Domain nunca dependa de
MediatR) solo si el guardado tuvo éxito; `AssetApprovalReactionHandler` como primer y único suscriptor en
V1 (`ApprovalCompleted`/`ApprovalRejected` → mueve el `Asset` al estado correspondiente); dos consumidores
reales conectados — baja de activos (`RequestAssetDecommissionCommand`, mueve a `PendingDecommission` de
inmediato) y disposición (`RequestAssetDisposalCommand`, parametrizado venta/donación/destrucción, sin
estado intermedio porque `Decommissioned` ya es un reposo válido); segundo mecanismo de firma
(`SignatureRecord.Mechanism = DrawnSignature`, imagen PNG capturada en `<canvas>`, junto al
`TypedConfirmation` de F3 — ver ADR 0005/0006); `Template`/`TemplateVersion` como catálogo versionado de
texto simple, sin motor de renderizado (depende de Blob Storage/Documents, F8, no construido). 136 pruebas
acumuladas (119 unitarias, 17 de integración con SQL Server real, incluyendo los flujos E2E "solicitar
baja → aprobar → dado de baja", "solicitar baja → rechazar → vuelve a InWarehouse" y la prohibición de
autoaprobación).

**Corrección real al grafo de `AssetStateMachine`**: F2 nunca modeló una salida desde `PendingDecommission`
distinta de `Decommissioned` — una solicitud de baja rechazada no tenía a dónde volver. Se agregó
`PendingDecommission → InWarehouse`, con su propia prueba (mismo patrón que la corrección de ADR 0004).

**Bug real encontrado y corregido durante la verificación**: las pruebas de integración compartían una
sola base de datos entre varios `[Fact]` de la misma clase (`IClassFixture`) y cada una creaba su
`ApprovalFlowDefinition` con `CompanyId: null` (alcance global) — una prueba podía "heredar" el flujo
creado por otra prueba anterior, con un rol distinto al esperado, y fallar con un 422 confuso ("no tienes
un rol elegible") en vez de completarse. No es un defecto de producción: se corrigió escribiendo cada
flujo de prueba con `CompanyId` específico de esa prueba, evitando la colisión en el `fallback` global de
`ApprovalCoordinator`.

**Frontend**: `/approval-flows` (admin: listar, crear con selector de roles ordenable para modo
secuencial, activar/desactivar), `/my-approvals` (**autoservicio**, igual que "Mis asignaciones" —
cualquier usuario autenticado ve sus aprobaciones pendientes por rol y decide con firma escrita o
dibujada), `/approvals/[id]` (detalle con historial de decisiones); botones "Solicitar baja"/"Solicitar
disposición" en el detalle de cada activo (visibles solo cuando el estado actual lo permite);
`/templates`, `/templates/new`, `/templates/[id]` (agregar versión, activar/desactivar). Verificado con
Playwright y una sesión real — en el momento de construir F4, la cuenta de prueba disponible no tenía
`Approvals.Configure`/`Assets.Decommission` todavía, así que el flujo completo de aprobación quedó
verificado por las pruebas de integración E2E, no visualmente con esa cuenta. Desde entonces se le
otorgaron los 68 permisos sembrados (instrucción permanente: cualquier permiso nuevo se le asigna sin
esperar a que se pida) — ver el estado de F5 para la verificación visual ya hecha con acceso completo.

**Pendiente** (no bloqueante): sin sustitución temporal ni escalamiento automático (V1.2 explícito en C4 —
el esquema ya los admite sin migrar datos, ver ADR 0006); `GetMyPendingApprovalsQuery` materializa las
instancias pendientes de la empresa en memoria para evaluar de quién es el turno en modo secuencial (no
se traduce limpiamente a SQL sobre una colección de roles) — aceptable al volumen de V1, a revisar si
crece; F3 no se retocó para ofrecer el mecanismo de firma dibujada en sus propios flujos (ver arriba);
`Template` es un catálogo de texto sin generación de documentos — depende de F8.

## Estado de F5 (Transferencias entre empresas)

A diferencia de F3/F4, F5 no construyó primitivas nuevas — **compuso** lo que ya existía: el motor de
aprobaciones genérico de F4 y el `Movement`/firma de F3, en un agregado `Transfer` nuevo. Coincide
exactamente con la dependencia que el propio roadmap ya fijaba ("F5 | F3, F4").

**Tensión real de diseño resuelta (ADR 0007)**: `multi-company.md` decía que la transferencia preserva
"folio e historial general del activo", pero `InternalFolio` es único **solo por empresa** (ADR 0004) —
preservarlo tal cual podría colisionar con el folio de un activo no relacionado en la empresa destino. Se
resolvió regenerando el folio en la secuencia de la empresa destino al completar la recepción, mientras
que `AssetTag.Code` (global, ADR 0004) nunca cambia — es la identidad que de verdad sobrevive, validando
que ADR 0004 se diseñó bien para este caso exacto.

**Backend** (con pruebas): `Transfer` (agregado: `PendingApproval → {Rejected, Cancelled, InTransit} →
Completed`; `Depart` colapsa "aprobado" + "salida" en un solo paso porque nada actúa sobre un estado
intermedio); `Asset.CompleteCrossCompanyTransfer` — **el único método que puede reasignar `CompanyId`**,
cumpliendo por primera vez la regla 3 de `CLAUDE.md`; cada transferencia genera **dos** `Movement` (uno
por empresa, `CrossCompanyTransferOut`/`In`) porque el filtro de consulta global es por una sola
`CompanyId` por fila — `Transfer` es el único agregado del sistema con un filtro de consulta con **OR**
entre dos empresas. Reutiliza `IApprovalCoordinator` (F4) para la aprobación y `SignatureFactory` (F4)
para la firma de recepción — sin código nuevo en esas piezas. 154 pruebas acumuladas (134 unitarias, 20
de integración con SQL Server real, incluyendo el flujo E2E completo "solicitar → aprobar → `InTransit` →
recibir con firma → `CompanyId`/`InternalFolio` nuevos, `AssetTag.Code` sin cambios, dos `Movement`
visibles" y el rechazo del intento de recepción sin acceso a la empresa destino).

**Alcance V1 (decisiones documentadas, no omisiones)**: solo se transfieren activos en `InWarehouse` (no
`Assigned` — el grafo ya permite `Assigned → InTransit`, pero cerrar una asignación activa como parte de
la transferencia queda para una fase futura); la transferencia siempre llega a `InWarehouse` en destino,
nunca directo a `Assigned`; la ubicación queda sin asignar al completarse (se reutiliza
`RelocateAssetCommand` de F3 después, en vez de duplicar esa lógica aquí); solo la recepción lleva firma,
la salida es consecuencia automática de la aprobación.

**Frontend**: `/transfers` (listado — aparece si la empresa activa es origen o destino), `/transfers/new`
(selector de activo `InWarehouse` + empresa destino entre todas las del tenant + justificación),
`/transfers/[id]` (detalle; "Cancelar" si pendiente de aprobación; formulario de recepción con firma
escrita o dibujada si está en tránsito); botón "Solicitar transferencia" en el detalle de cada activo
`InWarehouse`. Verificado con Playwright y la sesión real de Francisco Pérez — primera fase donde su
cuenta ya tenía los 68 permisos desde el inicio, así que la verificación visual cubrió las pantallas
completas sin los 403 que limitaron F3/F4.

**Pendiente** (no bloqueante): la prohibición de autoaprobación (F4) implica que, con un solo usuario real
en el tenant de prueba, el ciclo completo solicitar→aprobar no puede probarse de punta a punta en el
navegador con una sola cuenta — las 20 pruebas de integración cubren esa combinación contra SQL Server
real; transferir un activo `Assigned` queda fuera de V1 (ver alcance arriba); sin reimpresión automática
de etiqueta tras completarse (queda como acción manual del administrador en la empresa destino).

## Estado de F6 (Mantenimientos + Refacciones/Consumibles)

Dependía solo de F3 (no de F4/F5), y así fue: conecta los estados `InMaintenance`/`Damaged` que
`AssetStateMachine` reservó desde F2, sin tocar el motor de aprobaciones ni transferencias. Conecta
además los 12 permisos `Maintenance.*`/`Warranties.*`/`SpareParts.*`/`Consumables.*` sembrados desde F1 y
sin usar hasta ahora — ninguna migración de permisos nueva en esta fase.

**Ambigüedades reales resueltas (ADR 0008)**: (1) `UnderWarranty` se deja **sin conectar** en V1 — el
pedido no detalla un flujo de "enviado a garantía con el proveedor" distinto del mantenimiento ya cubierto,
así que no se inventó uno; (2) `Warranties.*` se conecta a un agregado **nuevo** `Warranty` (no a los
campos planos que `Asset` ya tiene desde F2) porque un activo puede acumular varias coberturas a lo largo
de su vida; (3) cerrar una `MaintenanceOrder` **nunca** mueve el activo directamente a
`PendingDecommission` — esa arista existe en el grafo pero entrar sin pasar por
`RequestAssetDecommissionCommand` (F4) dejaría el activo "pendiente de baja" sin ninguna aprobación
esperando decisión; el resultado de cierre se restringe a `InWarehouse`/`Damaged`.

**Backend** (con pruebas): `MaintenanceOrder` (abrir mueve el activo a `InMaintenance`; cerrar exige
resultado + evidencia en texto, mismo placeholder que usó F4 mientras Documents/F8 no existe; checklist
opcional, snapshot de ítems al abrir); `MaintenanceChecklistDefinition`/`MaintenanceChecklistVersion`
(mismo patrón versión-inmutable que `Template`/`TemplateVersion`, F4); `Warranty` (cobertura de
garantía/soporte por activo, independiente de los campos de `Asset`); `SparePart` (serializada,
`InStock ↔ Installed → Disposed`, historial de instalación/retiro con `Id` propio — se descubrió durante
las pruebas que la ausencia de `.ValueGeneratedNever()` en esa entidad hacía que EF Core tratara una
instalación nueva como si fuera una actualización de una fila inexistente; corregido); `Consumable`/
`ConsumableStockMovement` (existencia solo cambia vía `Consumable.ApplyStockMovement`, invariante "nunca
negativo", cada cambio genera un movimiento inmutable con folio propio). 186 pruebas acumuladas (161
unitarias, 25 de integración con SQL Server real, incluyendo abrir+cerrar una orden con checklist,
instalar/retirar una refacción, y un movimiento de consumible que dejaría existencia negativa rechazado).

**Frontend**: `/maintenance-orders` (listado + nueva orden con checklist opcional + detalle con checklist
interactivo y formulario de cierre), `/maintenance-checklists` (catálogo versionado, calco de
`/templates`), `/warranties` (listado + alta + edición), `/spare-parts` (listado + alta + instalar/
retirar/dar de baja), `/consumables` (listado + alta + registrar movimiento + historial); botón "Abrir
orden de mantenimiento" en la ficha de cada activo `InWarehouse`/`Assigned`. El almacén para refacciones y
consumibles reutiliza el `OrgUnit` de tipo Almacén ya sembrado desde F1 (sin interfaz de dominio nueva).

**Pendiente** (no bloqueante): `UnderWarranty` sigue sin ninguna transición conectada (ver ADR 0008);
`Warranty` no está enlazada estructuralmente a una `MaintenanceOrder` de tipo garantía (el pedido no lo
pidió así); MTTR/MTBF a partir de los datos ya capturados aquí queda para F10 (Reportes).

## Estado de F7 (Solicitudes internas)

Dependía de F3, F4 y F6 (Assignment/Loan, motor de aprobaciones, MaintenanceOrder) y así fue: no introduce
primitivas nuevas — es el front door de autoservicio que, al aprobarse, dispara la misma lógica de dominio
que ya usan `CreateAssignmentCommand`/`CreateLoanCommand`/`OpenMaintenanceOrderCommand`, igual que F5
compuso F3+F4 para `Transfer`. Conecta los 3 permisos `Requests.*` sembrados desde F1 y sin usar — ninguna
migración de permisos nueva en esta fase.

**Ambigüedades reales resueltas (ADR 0009)**: (1) sin fase de "borrador" editable — `InternalRequest.Create`
entra directo en `PendingApproval`, mismo patrón de un solo paso que `Transfer`/`Loan`/`MaintenanceOrder`;
(2) cumplimiento **automático** al aprobarse, tal como pide literalmente el roadmap ("generar asignaciones/
movimientos automáticamente"), vía un nuevo `InternalRequestApprovalReactionHandler` (mismo esqueleto que
`TransferApprovalReactionHandler` de F5) que reconstruye la secuencia de dominio según
`InternalRequestType` sin reinvocar los comandos originales; (3) `Requests.Update` **queda sin usar en
V1** — una vez que no hay borrador editable y cancelar es autoservicio sin permiso (mismo criterio que
`CancelTransferCommand`), no apareció una acción real de "actualizar" que lo necesitara.

**Backend** (con pruebas): `InternalRequest` (`Type`: `AssetAssignment \| Loan \| Maintenance`; `Status`:
`PendingApproval → {Rejected, Cancelled, Fulfilled}`; precondición de activo por tipo — `InWarehouse` para
asignación/préstamo, `InWarehouse` o `Assigned` para mantenimiento, ya que reportar que tu propio equipo
asignado falla es el caso real más común); tres flujos de aprobación distintos
(`internal-request.asset-assignment/.loan/.maintenance`) para que un administrador pueda asignar
aprobadores diferentes según el tipo. 202 pruebas acumuladas (172 unitarias, 30 de integración con SQL
Server real, incluyendo el flujo E2E de los tres tipos — solicitar → aprobar evento real → verificar que
se creó exactamente el `Assignment`/`Loan`/`MaintenanceOrder` esperado y el activo cambió de estado —, el
rechazo sin efecto sobre el activo, y la cancelación por el propio solicitante).

**Frontend**: `/requests` (listado administrativo, `Requests.Read`, filtro por empresa activa) +
`/requests/[id]` (detalle); `/requests/new` (tipo → activo elegible según tipo + justificación + fecha de
devolución solo si `Loan`); `/my-requests` (autoservicio, calco de `/my-assignments`: lista propia con
botón "Cancelar" si `PendingApproval`). Reutiliza sin cambios la UI genérica de aprobaciones ya existente
(`/my-approvals`, `/approvals/[id]`) — `describeApprovalContext` gana un caso más ("Solicitud interna").

**Pendiente** (no bloqueante): mismo riesgo ya aceptado en F4/F5 — si el estado del activo cambió entre el
envío y la aprobación (p. ej. alguien más lo tomó), el reaction handler lanza una excepción de dominio y
la decisión de aprobación en sí ya quedó guardada; no es un problema nuevo de esta fase.

## Estado de F8 (Documentos, Notificaciones, Auditoría UI)

Transversal, como anticipaba el análisis — pero a diferencia de otras fases, aquí nada existía todavía:
ni `Document`/`Notification`/`AuditEntry`, ni el puerto de Blob Storage, ni `IAuditableCommand`, ni
`INotificationSender` (los tres últimos mencionados desde F0/F1 en el análisis, nunca construidos). Azurite
(emulador de Azure Storage) estaba provisto en `docker-compose.yml` desde F0 pero nunca conectado — F8 lo
conecta.

**Ambigüedades reales resueltas (ADR 0010)**: (1) Documentos usa streaming por la API en vez de URLs SAS,
allowlist de entidades limitado en V1 a `Asset`/`MaintenanceOrder`; (2) Notificaciones sin cola de
reintentos (el pedido lo pide pero no hay infraestructura de colas ni SMTP configurado) — correo es mejor
esfuerzo, nunca finge enviar lo que no envió; los disparadores son dos *reaction handlers* genéricos sobre
eventos de Approvals que cubren F4/F5/F7 automáticamente; (3) Auditoría sin diff de "valores antes/después"
(se sustituye por una traza real: comando + parámetros + resultado), marcada en ~25 comandos sensibles
representativos (no los ~60 totales).

**Descubrimiento real durante la construcción, con consecuencia de diseño (ADR 0010)**: el frontend nunca
ha enviado el encabezado `X-Active-Company-Id` en ninguna fase — cada pantalla, desde F1, pasa `companyId`
explícito en vez de depender de la "empresa activa" implícita que `multi-company.md` anticipó. Diseñar
`AuditEntry` alrededor de esa empresa activa la habría dejado permanentemente vacía y el filtro de
consulta planeado habría sido un colador (todo quedaría visible para cualquiera con el permiso,
filtrar-por-null no protege nada). Se corrigió antes de cerrar la fase: `AuditEntry` no lleva filtro de
consulta por empresa — mismo tratamiento que `Company`/`Role` (`Audit.Read` es un permiso administrativo
global, no acotado por empresa) — y `CompanyId` se registra por reflexión desde el propio comando cuando
está disponible, como dato de presentación, no como límite de acceso.

**Backend** (con pruebas): `IFileStorage` (Azure.Storage.Blobs, Azurite real en pruebas de integración vía
Testcontainers — no un fake); `Document` inmutable con descarga streameada; `INotificationSender` +
`IEmailSender` (SMTP real si `Smtp:Host` está configurado, si no un sender que solo registra en Serilog);
`ApprovalRequested` agregado a `ApprovalInstance.Create` (F4, cambio aditivo); `AuditBehavior` (pipeline de
MediatR) + `IAuditableCommand`. 220 pruebas acumuladas (183 unitarias, 37 de integración con SQL Server y
Azurite reales, incluyendo subir/listar/descargar un documento de verdad, notificación al aprobador
elegible y al solicitante tras la decisión, y una entrada de auditoría para un comando exitoso y otra para
uno fallido).

**Frontend**: `DocumentsPanel` reutilizable (carga + lista + descarga) montado en la ficha de activo y de
orden de mantenimiento — la descarga pasa por un *route handler* de Next.js (`/api/documents/[id]/content`)
que hace de proxy autenticado, ya que la API del backend no es alcanzable directamente desde el navegador;
`/notifications` (autoservicio, calco de `/my-assignments`); `/audit` (admin, `Audit.Read`, sin
`CompanySwitcher` — mismo criterio que `/companies`, con filtros de comando y fecha).

**Pendiente** (no bloqueante): extender el allowlist de documentos y el barrido de `IAuditableCommand` a
más entidades/comandos es agregar una constante o una línea de interfaz, no una limitación estructural;
sin contador de notificaciones no leídas en el encabezado (polish diferido); `docs/multi-company.md`
debería actualizarse para dejar de anticipar un uso de `X-Active-Company-Id` que ocho fases confirmaron
innecesario — se deja para una fase futura para no desviarse del alcance de F8.

## Estado de F9 (Importaciones/Exportaciones)

ADR 0003 (F0) ya había resuelto por adelantado la tensión central — procesamiento asíncrono desacoplado
vía `ImportBatch`/`IImportQueue` — así que F9 construyó todo lo que quedaba pendiente: el agregado, el
worker real, la validación/confirmación en dos fases, y la exportación (que resultó no necesitar nada de
eso, al ser de solo lectura). Conecta los 3 permisos `Imports.Read`/`Imports.Create`/`Exports.Create`
sembrados desde F1 y sin usar hasta ahora — ninguna migración de permisos nueva en esta fase.

**Ambigüedades reales resueltas (ADR 0011)**: (1) alcance V1 limitado a `Asset` (mismo criterio que F8
limitó Documentos a `Asset`/`MaintenanceOrder`); (2) CSV para importar (datos estructurados) vs Excel/PDF
para exportar (lectura humana); (3) validación en dos fases con confirmación explícita del usuario
(`AllOrNothing`/`ValidRowsOnly`), re-validando cada fila también al confirmar — el estado pudo cambiar
desde la vista previa, mismo riesgo ya aceptado en F4/F5/F7; (4) duplicados detectados por número de serie
dentro de la empresa; (5) el worker no reinvoca `CreateAssetCommand` vía `ISender` (no hay `HttpContext`
en un `BackgroundService`) — llama los mismos métodos de dominio directamente, mismo criterio que
`InternalRequestApprovalReactionHandler` (F7); (6) PdfSharp elegido sobre QuestPDF por licencia (MIT sin
condición de ingresos), lo que exigió empaquetar una fuente (Liberation Sans, SIL OFL) porque PdfSharp 6
no tiene resolutor de fuentes de plataforma dentro de un contenedor Linux.

**Descubrimiento real durante la construcción (ADR 0011)**: F9 es el primer `BackgroundService` de este
proyecto que necesita leer `Asset`/`OrgUnit`/`ImportBatch` fuera de un request HTTP real — su *scope* de
DI no tiene `HttpContext`, así que el filtro de consulta por empresa (que depende de
`ICurrentCompanyContext.AccessibleCompanyIds`, resuelto de ahí) habría devuelto cero filas silenciosamente
en cada lectura del worker. Se corrigió con el mismo criterio que ADR 0010 ya fijó para Auditoría: toda
lectura dentro de `ImportBatchProcessor` usa `IgnoreQueryFilters()` explícitamente y filtra por el
`CompanyId` propio del lote, nunca por una "empresa activa" que en ese contexto no existe.

**Backend** (con pruebas): `ImportBatch` (estados `Queued → Validating → Validated` → confirmación →
`Processing → Completed \| CompletedWithErrors \| Failed`, `Cancelled` antes de confirmar);
`IImportQueue` (`ChannelImportQueue` local, `AzureStorageQueueImportQueue` en producción, mismo contrato);
`ImportBatchBackgroundService` (worker único, idempotente sobre el `Status` persistido);
`AssetImportRowValidator` (misma validación que `CreateAssetCommand` más detección de duplicados);
`ExportAssetsQuery` (síncrono, streameado, ClosedXML/PdfSharp). 244 pruebas acumuladas (214 unitarias, 43
de integración con SQL Server y Azurite reales — el propio `ImportBatchBackgroundService` corriendo en el
host de pruebas, no un *fake* del worker — incluyendo validar con filas válidas/inválidas, confirmar en
ambos modos, cancelar, y exportar a Excel/PDF real).

**Frontend**: `/imports` (listado + estado), `/imports/new` (subir CSV + descargar plantilla por
categoría), `/imports/[id]` (reporte fila-por-fila, confirmar con selector de modo, cancelar, botón manual
"Actualizar" en vez de *polling* — mismo criterio que el resto de la app, ningún flujo async usa eso
todavía); botones "Exportar Excel"/"Exportar PDF" en `/assets`, vía un nuevo *route handler* proxy
(`/api/exports/assets`, mismo patrón que el proxy de documentos de F8).

**Pendiente** (no bloqueante): extender el allowlist de importación/exportación a otras entidades es
agregar un `RowValidator`/proyección análogos, no un cambio estructural; sin *polling* en `/imports/[id]`
(recarga manual, ver ADR 0011 decisión 9); MTTR/MTBF y KPIs de importación masiva quedan para F10.

## Estado de F10 (Reportes y paneles)

A diferencia de toda fase anterior, F10 no agregó ningún agregado ni tabla nueva — es el contexto
"Reporting" que el análisis inicial ya describía como proyecciones de solo lectura. Todos los datos que
pide el roadmap ya existían: `MaintenanceOrder.OpenedAtUtc/ClosedAtUtc` (MTTR/MTBF), `Warranty.EndDate`
(vencimientos), `Consumable.MinimumStock/CurrentStock` (existencias bajas — `MinimumStock` existe desde F6
y nunca se había usado), `Asset.Status`/`AssetCategoryId` (inventario). Sin migración en esta fase. Conecta
los 3 permisos `Reports.Read`/`Reports.ReadConsolidated`/`Reports.Export` sembrados desde F1 y sin usar.

**Ambigüedades reales resueltas (ADR 0012)**: (1) `Reports.Read` vs `Reports.ReadConsolidated` se resuelven
por la forma del propio request — `CompanyId` presente exige el primero (una empresa, membresía validada
como siempre), ausente exige el segundo (agrega sobre todas las empresas accesibles); (2) cuatro paneles
V1 sin inventar KPIs no pedidos: resumen de inventario (sin exportar, es orientación), MTTR/MTBF (con
desglose por categoría), garantías por vencer (ventana configurable, incluye las ya vencidas), existencias
bajas (bajo el mínimo definido); (3) exportar reutiliza la infraestructura de F9 — se extrajo
`TabularFileBuilder` de `ExportAssetsQueryHandler` a una clase compartida, sin cambiar su comportamiento
(las 31 pruebas de exportación de F9 pasaron sin tocarse tras el refactor).

**Dos bugs reales de traducción SQL Server, encontrados solo contra SQL Server real (no en InMemory) y
documentados en ADR 0012**: `GetLowStockConsumablesQuery` proyectaba `MinimumStock!.Value` (no traduce
aunque el `Where` ya garantice no-nulo) y ordenaba por una propiedad de un registro ya proyectado (tampoco
traduce) — corregido con `?? 0` y ordenando antes de proyectar. `GetMaintenanceKpisQuery` comparaba un enum
mapeado a `nvarchar` con `==` dentro de una proyección, la misma causa raíz exacta del bug ya documentado
en "Estado de F3" (`SqlException` del operador `^` entre `nvarchar`) — corregido seleccionando el enum
crudo y comparando en memoria.

**Backend** (con pruebas): cuatro queries de solo lectura (`GetInventorySummaryQuery`,
`GetMaintenanceKpisQuery` con MTTR/MTBF calculados en memoria sobre las órdenes materializadas — mismo
criterio de volumen que `GetMyPendingApprovalsQuery`, ADR 0006 —, `GetExpiringWarrantiesQuery`,
`GetLowStockConsumablesQuery`) y tres exportaciones (`Export*Query`, Excel/PDF vía `TabularFileBuilder`
compartido). 267 pruebas acumuladas (221 unitarias, 46 de integración con SQL Server real — incluyendo
aislamiento por empresa, agregación consolidada entre dos empresas reales, y las tres exportaciones
produciendo archivos válidos).

**Frontend**: `/reports` — cuatro tarjetas (inventario, MTTR/MTBF con tabla por categoría, garantías por
vencer con filtro de ventana y exportar, existencias bajas con exportar), alternador "Ver consolidado" que
quita `companyId` de la URL (visible siempre, sin gating de UI por permiso — el 403 se maneja igual que en
el resto de la app), enlaces de exportación vía un nuevo *route handler* proxy genérico
(`/api/reports/export?report=...`, mismo patrón que `/api/exports/assets` de F9).

**Pendiente** (no bloqueante): sin caché ni tabla de proyección materializada (cada consulta agrega en el
momento; documentado como punto de extensión si el volumen real lo exige); extender los paneles a más
KPIs (p. ej. costo total por categoría) es agregar una query más, no un cambio estructural.

## Estado de F11 (Búsqueda avanzada)

Como F10, sin bounded context ni agregado propio — es una capa transversal de consulta sobre 8 tipos de
entidad ya existentes. Sin migración en esta fase, sin permiso nuevo: cada tipo reutiliza su propio
`{Módulo}.Read` ya sembrado desde F1.

**Ambigüedades reales resueltas (ADR 0013)**: (1) autorización por tipo de resultado, no por el request
completo — `GlobalSearchQuery` no implementa `IRequiresPermission` (como `GetMeQuery`); el handler llama
`IPermissionChecker.HasPermissionAsync` imperativamente por cada uno de los 8 tipos, incluyendo solo los
que el usuario puede ver, sin que la búsqueda entera falle con 403 por faltarle un permiso de un solo
tipo — primer uso imperativo de `IPermissionChecker` fuera del pipeline de MediatR en el proyecto; (2)
ocho tipos buscables (`Asset`, `Movement`, `MaintenanceOrder`, `Warranty`, `SparePart`, `Consumable`,
`InternalRequest`, `User`) — `Document` y los catálogos administrativos quedan fuera de V1 (documentado,
no omitido); (3) `Movement` no tiene página de detalle propia — sus resultados enlazan al `Asset`
relacionado vía un par `LinkEntityType`/`LinkEntityId` separado del `EntityType`/`EntityId` del propio
resultado, así el frontend resuelve un único mapeo tipo→ruta sin casos especiales; (4) `CompanyId`
opcional sin una segunda categoría de permiso — ausente busca en todas las empresas accesibles, presente
acota a una (validando membresía como siempre); (5) sin *type-ahead* — un formulario GET simple en el
encabezado, mismo criterio de "sin *polling*/JS en vivo" que ADR 0011 ya fijó para no introducir un
patrón nuevo sin que el pedido lo exigiera.

**Backend** (con pruebas): `GlobalSearchQuery` con 8 sub-consultas secuenciales sobre el mismo `DbContext`
(nunca `Task.WhenAll` sobre él), cada una evitando desde el diseño el bug de traducción a SQL Server que
F10 ya documentó (ADR 0012) — ningún enum mapeado a `nvarchar` se compara ni se ordena dentro de una
proyección traducida, se selecciona crudo y se formatea después de materializar. 275 pruebas acumuladas
(226 unitarias — incluyendo un `FakePermissionChecker` nuevo en `TestSupport` para probar el filtrado
por tipo —, 49 de integración con SQL Server real, incluyendo aislamiento de resultados por permiso y por
empresa).

**Frontend**: cuadro de búsqueda en `AppHeader` (formulario GET, visible en cualquier pantalla) + `/search`
(resultados agrupados por tipo, enlace "Acotar a una empresa" para pasar de todas las empresas accesibles
a una sola).

**Pendiente** (no bloqueante): agregar un noveno tipo buscable (p. ej. `Document` resolviendo su padre
polimórfico para el enlace) es una sub-consulta más siguiendo el mismo patrón, no un cambio estructural.

## Estado de F12 (Privacidad/retención, hardening, carga)

Primera fase puramente transversal — sin pantalla nueva para un usuario de negocio, cierra tres
compromisos que el análisis inicial dejó explícitamente para esta fase (ver ADR 0014 para el razonamiento
completo de cada decisión).

**Privacidad/retención**: `docs/privacy-retention.md` (nuevo) documenta el inventario real de PII por
entidad y qué se retiene indefinidamente (`Movement`/`AuditEntry`/`SignatureRecord`, el registro legal de
custodia) vs. qué se purga (`Notification`, ya documentada en F8 como "efímera por diseño"). Dos
mecanismos reales, no solo documentación: `User.Anonymize()` (irreversible, distinto de `Deactivate()` ya
existente, gated por `Users.Update` ya sembrado) desde `/users/{id}`, y
`NotificationRetentionBackgroundService` (mismo patrón de *hosting* que `ImportBatchBackgroundService`,
F9) purgando notificaciones con más de 90 días (configurable) una vez al día.

**Hardening**: *rate limiting* real (`Microsoft.AspNetCore.RateLimiting`, ya en el framework, 300
solicitudes/10s por usuario/IP, generoso a propósito, con `Retry-After`) y encabezados de seguridad
(`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, HSTS fuera de desarrollo) en la API;
mismo conjunto más una `Content-Security-Policy` pragmática en el frontend. Ver
`docs/security/hardening.md`.

**Carga**: `LoadTests.cs` (nuevo, dentro de `AssetManagement.IntegrationTests`, reutilizando
`ApiWebApplicationFactory` — sin k6 real, sin proyecto nuevo, "equivalente" tal como `docs/testing.md` ya
autorizaba) siembra 2,000 activos y ejecuta 5 escenarios concurrentes reales (listar, detalle, búsqueda
global, resumen de inventario, alta concurrente de activos — el primer estrés real sobre
`EfFolioGenerator` bajo escritura concurrente del proyecto), con cero errores y latencias documentadas en
`docs/performance.md`. **Limitación explícita**: valida concurrencia moderada en un proceso local, no la
escala de referencia completa del pedido (250k activos, 2M movimientos, 500 usuarios concurrentes) — esa
validación requiere infraestructura Azure real y queda para F13, tal como `docs/testing.md` ya lo
anticipaba desde F0 ("ejecutado en fase F12 contra ambiente `test`", un ambiente que F13 todavía no
construye).

**Backend** (con pruebas): 284 pruebas acumuladas (230 unitarias, 54 de integración con SQL Server real
— incluyendo el límite de solicitudes disparando un 429 real con `Retry-After`, la anonimización de
punta a punta, la purga de notificaciones por antigüedad, y la prueba de carga con cero errores y folios
únicos bajo escritura concurrente). Una sola migración (`AnonymizedAtUtc` en `Users`).

**Frontend**: sección "Privacidad" nueva en `/users/{id}` con el botón "Anonimizar (irreversible)",
encabezados de seguridad activos en cada respuesta.

**Pendiente** (no bloqueante, explícitamente fuera de alcance de V1): WAF/DDoS administrado y CSP basada
en *nonces* (F13/infraestructura real); validación de carga a la escala de referencia completa (F13);
purga configurable de `Movement`/`AuditEntry` (requiere una decisión de negocio explícita, no un valor
por defecto).

## Estado de F13 (Azure/CI-CD productivo)

Distinta en naturaleza de F0-F12: el propio roadmap ya advertía "Requiere información de Azure (§18)" —
información que nunca llegó en esta conversación (sin suscripción de Azure, sin repositorio de GitHub
remoto conectado — `git remote -v` no devuelve nada). `docs/deployment.md` (F0) ya lo anticipaba: *"La
infraestructura como código se construye parametrizada para no bloquear el desarrollo del código de
aplicación"*. Ver ADR 0015 para el razonamiento completo de cada decisión.

**Entregado, completo y correcto**: toda la infraestructura como código (`infrastructure/bicep/` — App
Service Linux por contenedores reutilizando las imágenes Docker ya existentes, Azure SQL con
autenticación exclusiva de Entra ID sin contraseña SQL alguna, Storage con cadena de conexión referenciada
desde Key Vault, Container Registry por ambiente con promoción de imágenes sin reconstruir, Application
Insights/Log Analytics) y los flujos de CI/CD (`.github/workflows/` — `ci.yml` espejo exacto de los
comandos que ya se corren localmente, `codeql.yml`, `cd.yml` con OIDC y aprobación manual solo en `prod`
vía GitHub Environments — más `.github/dependabot.yml`).

**No entregado, honestamente**: un despliegue real, o una ejecución real de GitHub Actions — no hay a
dónde desplegar ni qué repositorio remoto disparar. Verificado en su lugar con las herramientas de
sintaxis obtenibles en este entorno sin una suscripción: `bicep build`/`build-params` (CLI standalone,
sin errores ni advertencias) y `actionlint` (sin hallazgos). `docs/deployment.md` incluye la guía paso a
paso completa de qué hacer con información real de Azure/GitHub, incluyendo los pasos que Bicep
deliberadamente no automatiza (usuario SQL para la identidad administrada, secretos reales en Key Vault,
migraciones de EF Core).

**Pendiente** (no bloqueante, documentado): Private Endpoints/VNet, WAF de borde, protección DDoS
administrada (`docs/security/hardening.md`); automatizar el usuario SQL vía `deploymentScripts`;
aplicar migraciones de EF Core como parte de `cd.yml`.

## Explícitamente fuera de alcance de V1 (documentado, no implementado)

- Depreciación, amortización, valor en libros, vida útil financiera, revalorizaciones, contabilidad de
  activos fijos, integraciones financieras con ERP, reportes fiscales.
- Alta disponibilidad formal, SLA/SLO/RTO/RPO formales, recuperación regional automatizada, replicación
  geográfica.
- Integración con Microsoft Teams (se deja abstracción de canal de notificaciones lista para incorporarla).
- Multi-tenant con tenants independientes de Entra ID (V1 es un único tenant, multiempresa lógica).

## Puntos de extensión previstos (sin código sin uso en V1)

- `INotificationChannel` — abstracción ya usada por Email/In-app; agregar Teams es implementar una interfaz
  nueva, no reescribir el módulo.
- `ApprovalFlowDefinition.ApproverRoleIds`/sustitución/escalamiento — la resolución de elegibilidad ya es
  dinámica (consulta `UserRole` en el momento de decidir, ver ADR 0006), así que sustitución temporal y
  escalamiento (V1.2) se implementan como formas de ampliar temporalmente "quién cuenta como el rol X",
  sin migrar `ApprovalFlowDefinition`/`ApprovalInstance`; solo falta el mecanismo de sustitución en sí y
  el job de escalamiento automático.
- Campos financieros informativos (costo, proveedor, factura) ya existen en `Asset` para V1 como *datos*, sin
  ningún cálculo — una futura capa de depreciación puede construirse sobre ellos sin migrar el modelo base.
- `Import/Export` desacoplado por cola — permite escalar a Service Bus o Functions sin cambiar el contrato de
  `ImportBatch`.
