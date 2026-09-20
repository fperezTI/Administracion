# 8. Gestión de usuarios y permisos

## Modelo de permisos

El sistema usa un modelo de permisos globales con formato `{Módulo}.{Acción}`. "Globales" significa que un permiso habilita una acción en **todas** las empresas a las que el usuario tenga acceso — los permisos nunca se acotan por empresa; lo que sí se acota por empresa es a **cuáles** empresas puede aplicar esa acción un usuario (ver §6, Paso 3, y la sección "Usuarios" más abajo).

El catálogo de permisos es fijo: se carga una sola vez mediante una migración de base de datos y, en esta versión del sistema, no puede ampliarse ni editarse desde la interfaz — solo se consulta. Nuevas versiones del sistema pueden agregar permisos nuevos al catálogo (por ejemplo, para módulos que hoy todavía no exigen ningún permiso propio).

La siguiente tabla lista el catálogo completo de permisos vigente (69 permisos en total, el mismo número que recibe automáticamente el rol "Super Administrador" en el bootstrap descrito en §6):

| Módulo | Permiso | Qué habilita | ¿Se exige hoy? |
|---|---|---|---|
| Companies | `Companies.Read` | Ver el catálogo de empresas. | Sí |
| Companies | `Companies.Create` | Registrar una nueva empresa. | Sí |
| Companies | `Companies.Update` | Activar o desactivar una empresa existente. | Sí |
| Structure | `Structure.Read` | Consultar la estructura organizacional de una empresa. | Sí |
| Structure | `Structure.Create` | Crear unidades organizacionales nuevas. | Sí |
| Structure | `Structure.Update` | Mover unidades en la jerarquía y activarlas/desactivarlas. | Sí |
| Roles | `Roles.Read` | Consultar el catálogo de roles. | Sí |
| Roles | `Roles.Create` | Crear un rol nuevo. | Sí |
| Roles | `Roles.Update` | Renombrar un rol y activarlo/desactivarlo. | Sí |
| Roles | `Roles.Duplicate` | Duplicar un rol existente junto con toda su matriz de permisos. | Sí |
| Permissions | `Permissions.Read` | Consultar el catálogo de permisos disponibles. | Sí |
| Permissions | `Permissions.Manage` | Editar la matriz de permisos de un rol. | Sí |
| Users | `Users.Read` | Consultar el listado y el perfil de los usuarios. | Sí |
| Users | `Users.Update` | Anonimizar el perfil de un usuario. | Sí |
| Users | `Users.ManageRoles` | Asignar o quitar roles a un usuario. | Sí |
| Users | `Users.ManageCompanies` | Otorgar o revocar el acceso de un usuario a una empresa. | Sí |
| Assets | `Assets.Read` | Consultar el inventario de activos. | Sí |
| Assets | `Assets.Create` | Dar de alta un activo. | Sí |
| Assets | `Assets.Update` | Editar los datos de un activo. | Sí |
| Assets | `Assets.Decommission` | Dar de baja un activo. | Sí |
| Assignments | `Assignments.Read` | Consultar asignaciones de activos. | Sí |
| Assignments | `Assignments.Create` | Registrar una asignación. | Sí |
| Assignments | `Assignments.Update` | Modificar una asignación existente. | Sí |
| Returns | `Returns.Read` | Consultar devoluciones. | Sí |
| Returns | `Returns.Create` | Registrar la devolución de un activo asignado. | Sí |
| Loans | `Loans.Read` | Consultar préstamos. | Sí |
| Loans | `Loans.Create` | Registrar un préstamo temporal. | Sí |
| Loans | `Loans.Update` | Modificar un préstamo existente. | Sí |
| Transfers | `Transfers.Read` | Consultar transferencias. | Sí |
| Transfers | `Transfers.Create` | Iniciar una transferencia de activo (única vía para cambiar la empresa de un activo). | Sí |
| Transfers | `Transfers.Update` | Modificar/avanzar el estado de una transferencia. | Sí |
| Movements | `Movements.Read` | Consultar el historial de movimientos de inventario. | Sí |
| Requests | `Requests.Read` | Consultar solicitudes internas. | Sí |
| Requests | `Requests.Create` | Crear una solicitud interna. | Sí |
| Requests | `Requests.Update` | Actualizar una solicitud interna. | Sí |
| Maintenance | `Maintenance.Read` | Consultar mantenimientos y checklists. | Sí |
| Maintenance | `Maintenance.Create` | Crear una orden o checklist de mantenimiento. | Sí |
| Maintenance | `Maintenance.Update` | Actualizar una orden de mantenimiento. | Sí |
| Warranties | `Warranties.Read` | Consultar garantías registradas. | Sí |
| Warranties | `Warranties.Create` | Registrar una garantía. | Sí |
| Warranties | `Warranties.Update` | Actualizar una garantía. | Sí |
| SpareParts | `SpareParts.Read` | Consultar refacciones. | Sí |
| SpareParts | `SpareParts.Create` | Registrar una refacción. | Sí |
| SpareParts | `SpareParts.Update` | Actualizar una refacción. | Sí |
| Consumables | `Consumables.Read` | Consultar consumibles. | Sí |
| Consumables | `Consumables.Create` | Registrar un consumible. | Sí |
| Consumables | `Consumables.Update` | Actualizar un consumible. | Sí |
| Documents | `Documents.Read` | Consultar documentos adjuntos. | Sí |
| Documents | `Documents.Create` | Adjuntar un documento. | Sí |
| Templates | `Templates.Read` | Consultar plantillas. | No verificado en este análisis |
| Templates | `Templates.Create` | Crear una plantilla. | No verificado en este análisis |
| Templates | `Templates.Update` | Actualizar una plantilla. | No verificado en este análisis |
| Reports | `Reports.Read` | Ver reportes de las empresas propias. | Sí |
| Reports | `Reports.ReadConsolidated` | Ver el reporte consolidado entre varias empresas. | Sí |
| Reports | `Reports.Export` | Exportar reportes. | Sí |
| Audit | `Audit.Read` | Consultar el historial de auditoría. | Sí |
| Catalogs | `Catalogs.Read` | Consultar catálogos, incluidas las categorías de activos y sus campos técnicos. | Sí |
| Catalogs | `Catalogs.Create` | Crear una categoría de activos. | Sí |
| Catalogs | `Catalogs.Update` | Actualizar una categoría de activos o agregar campos técnicos. | Sí |
| Imports | `Imports.Read` | Consultar lotes de importación masiva. | Sí |
| Imports | `Imports.Create` | Ejecutar una importación masiva. | Sí |
| Exports | `Exports.Create` | Generar una exportación de datos. | Sí |
| Approvals | `Approvals.Read` | Consultar flujos y solicitudes de aprobación. | Sí |
| Approvals | `Approvals.Configure` | Configurar flujos de aprobación. | Sí |
| Approvals | `Approvals.Approve` | Aprobar una solicitud pendiente. | Sí (sin flujo que lo use en la práctica todavía) |
| Approvals | `Approvals.Reject` | Rechazar una solicitud pendiente. | Sí (sin flujo que lo use en la práctica todavía) |
| Configuration | `Configuration.Read` | Consultar la configuración general del sistema. | Sin controles de acceso activos todavía |
| Configuration | `Configuration.Update` | Modificar la configuración general del sistema. | Sin controles de acceso activos todavía |
| Dashboards | `Dashboards.ViewExecutive` | Ver el dashboard ejecutivo consolidado. | Sí |

**Nota:** el menú lateral de "Administración" muestra siempre todas sus opciones a cualquier usuario, sin ocultar las que no puede usar; el control real de acceso ocurre al entrar a cada pantalla — si falta el permiso, la pantalla misma responde con un mensaje de "no tienes permiso" (ver ejemplos en §6). Esto significa que ver una opción en el menú no garantiza poder usarla.

---

## Roles

Los roles son paquetes de permisos con un nombre y una descripción. Se administran desde **Administración → Roles** (`/roles`).

**Listado.** Muestra Nombre (con enlace al detalle del rol), Descripción, cantidad de Permisos asignados y Estado. El subtítulo de la pantalla recuerda: "Los permisos son globales — la empresa no acota qué puede hacer un rol." Si todavía no hay roles: "Todavía no hay roles." Si falta el permiso: "No tienes permiso para consultar roles (Roles.Read)."

Nada más desplegar el sistema, el único rol que existe es el que se creó automáticamente en el bootstrap: **"Super Administrador"**, con la descripción "Rol de arranque con todos los permisos, creado automáticamente para el primer usuario del sistema." y sus 69 permisos activos.

**Crear un rol.** Desde "Nuevo rol" (`/roles/new`):

| Campo | Descripción | Obligatorio | Validaciones |
|---|---|---|---|
| Nombre | Nombre del rol (p. ej. "Técnico de soporte"). | Sí | Máximo 100 caracteres. Mensaje si falta: "El nombre es obligatorio." (Dominio: "El nombre del rol es obligatorio.") |
| Descripción | Texto libre que explica el propósito del rol. | No | Máximo 500 caracteres. |

Un rol recién creado no tiene ningún permiso asignado — deben agregarse desde su pantalla de detalle.

**Detalle de un rol** (`/roles/[id]`). *[Captura pendiente: pantalla de detalle de rol]* Reúne tres funciones independientes:

1. **Renombrar.** Formulario con Nombre y Descripción que actualiza el rol (permiso `Roles.Update`). Al guardar, el sistema confirma con: "Guardado."
2. **Duplicar.** Campo "Duplicar como" con el nombre del nuevo rol; al confirmar, crea un rol nuevo con **una copia completa** de la matriz de permisos del rol de origen (permiso `Roles.Duplicate`). Es la forma recomendada de crear variantes de un rol existente sin tener que marcar permiso por permiso otra vez. Si se intenta duplicar sin escribir un nombre: "Escribe un nombre para el rol duplicado."
3. **Matriz de permisos.** Casillas de verificación agrupadas por módulo, una por cada permiso del catálogo (permiso `Permissions.Manage`). Al guardar, **se reemplaza la matriz completa** del rol por la selección actual — no es una edición incremental, así que conviene revisar todas las casillas del módulo antes de guardar, no solo la que se quiere cambiar. Al guardar exitosamente: "Permisos guardados."

Además, desde esta misma pantalla se puede **activar o desactivar** el rol.

![Formulario de alta de rol](screenshots/055_roles_nuevo.png)

**Activar/Desactivar.** Un rol nunca se borra físicamente del sistema — desactivarlo es la única forma de retirarlo de circulación. Un rol desactivado sigue existiendo (y sigue mostrando su historial en auditoría) pero deja de poder asignarse a usuarios nuevos.

![Listado de roles](screenshots/054_roles_lista.png)

---

## Usuarios

Los perfiles de usuario **nunca se crean a mano**. Se crean automáticamente la primera vez que una persona inicia sesión con su cuenta de Microsoft Entra ID (ver §6, Paso 1, para el caso especial del primer usuario del sistema). Por eso, la pantalla de listado de usuarios (`/users`) no tiene botón "Nuevo usuario", y cuando todavía no hay ningún usuario cargado — situación que en la práctica solo existe antes del primer login — muestra: "Todavía no hay usuarios (se crean automáticamente en el primer inicio de sesión)."

![Listado de usuarios](screenshots/056_usuarios_lista.png)

El listado muestra: Nombre (con enlace al detalle), Correo, Estado y Último acceso.

**Detalle de un usuario** (`/users/[id]`). *[Captura pendiente: pantalla de detalle de usuario]* Desde aquí se administra todo lo que un administrador puede hacer sobre un usuario ya existente — no hay edición manual de su nombre o correo, porque esos datos provienen directamente de Entra ID:

- **Roles asignados.** Una lista de los roles que tiene el usuario, cada uno con un botón "Quitar" (permiso `Users.ManageRoles`), y un selector con botón "Asignar" que solo ofrece los roles **activos** que el usuario todavía no tiene.
- **Empresas con acceso.** El mismo patrón, con "Otorgar" y "Revocar" (permiso `Users.ManageCompanies`). **Este es el mecanismo exacto que se usa en el Paso 3 de §6** para que el primer superadministrador se dé acceso a la primera empresa que creó, y es también la forma de dar de alta a cualquier usuario nuevo en una empresa.
- **Anonimizar (irreversible).** Botón destructivo (permiso `Users.Update`) que ejecuta la acción sin pedir una confirmación adicional en pantalla, así que debe usarse con cuidado. El texto junto al botón explica: "Elimina permanentemente el nombre y correo reales de este perfil... No afecta el historial de auditoría ya registrado. Esta acción no se puede deshacer." Si el usuario ya fue anonimizado antes, el botón se reemplaza por el texto: "Anonimizado el {fecha}. El nombre y correo reales ya no están disponibles."

**Nota sobre auditoría:** asignar un rol u otorgar acceso a una empresa queda registrado en el historial de auditoría; sin embargo, **quitar** un rol o **revocar** el acceso a una empresa no genera hoy un registro de auditoría equivalente. Si su organización necesita trazabilidad completa de estos cambios, lleve un registro complementario fuera del sistema hasta que esta asimetría se corrija.

**Nota sobre anonimización:** anonimizar a un usuario es distinto de desactivarlo. Anonimizar borra permanentemente su nombre y correo reales del perfil (útil, por ejemplo, ante una baja de personal y una solicitud de derecho al olvido), pero **no reescribe los registros de auditoría ya generados** — el nombre que aparece en auditoría por acciones pasadas de esa persona queda tal como se registró en su momento, de forma intencional, para no perder la trazabilidad histórica.

---

## Perfiles de acceso: roles configurables, no predefinidos

El sistema **no incluye roles predefinidos de fábrica** más allá del rol "Super Administrador" que se genera una única vez en el bootstrap (§6, Paso 1). Cualquier otro rol — "Administrador de Activos", "Encargado de Almacén", "Auditor", "Técnico de soporte", o el nombre que la organización prefiera — **debe crearlo un administrador** combinando los permisos del catálogo de la forma que tenga sentido para su estructura de trabajo, tal como se describe en "Roles" más arriba.

Lo que puede hacer una persona en el sistema depende exclusivamente de la combinación de permisos que tenga su rol (o roles) asignados, y de a qué empresas tenga acceso. A modo de referencia, estas son combinaciones de permisos **razonables pero enteramente de ejemplo** — no vienen incluidas en el sistema y deben configurarse manualmente si se desean:

| Ejemplo de configuración (nombre libre) | Combinación de permisos ilustrativa | Qué podría hacer con esos permisos |
|---|---|---|
| "Administrador de Activos" (ejemplo) | `Assets.*`, `Assignments.*`, `Transfers.*`, `Movements.Read`, `Warranties.*`, `Documents.*`, `Reports.Read` | Dar de alta, editar y dar de baja activos; asignarlos y transferirlos entre empresas o unidades; consultar el historial de movimientos; registrar garantías; adjuntar documentación; ver reportes de sus empresas. |
| "Encargado de Almacén" (ejemplo) | `Consumables.*`, `SpareParts.*`, `Movements.Read`, `Assignments.Read`, `Requests.Read`, `Requests.Create` | Gestionar el stock de consumibles y refacciones; consultar movimientos y asignaciones sin poder modificarlas; crear solicitudes internas de reposición. |
| "Auditor / solo lectura" (ejemplo) | `Audit.Read`, `Reports.Read`, `Reports.ReadConsolidated`, `Assets.Read`, `Movements.Read`, `Assignments.Read` | Consultar el historial de auditoría y reportes, incluido el consolidado entre empresas, así como el inventario y sus movimientos — sin permiso para crear ni modificar nada. |

Al crear roles propios, tenga en cuenta dos particularidades del catálogo documentadas más arriba: las categorías de activos se controlan con el permiso `Catalogs.*` (no existe un permiso `AssetCategories.*` dedicado), y `Reports.ReadConsolidated` está separado de `Reports.Read` precisamente para poder darle a alguien acceso a los reportes de sus propias empresas sin exponerle el consolidado de toda la organización.
