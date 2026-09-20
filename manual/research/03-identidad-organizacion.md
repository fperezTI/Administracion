# Reporte exhaustivo: Módulo de Organización e Identidad/RBAC

## 0. Resumen del hallazgo más importante

**`docs/getting-started.md` (líneas 112-125) y `docs/security/authorization-rbac.md` están desactualizados respecto al código actual.** Afirman que "todavía no existe la UI de administración (roles, permisos, empresas)" y que el alta debe hacerse manualmente vía script/DB. Esto **ya no es cierto**:

- Existen y están **completamente funcionales de extremo a extremo**: `companies/new`, `companies`, `org-units`, `roles`, `roles/new`, `roles/[id]`, `users`, `users/[id]`, `asset-categories`, `asset-categories/new`, `asset-categories/[id]`.
- Además, `ProvisionOrUpdateUserCommand.cs` implementa un **bootstrap automático**: la primera cuenta que hace login en la vida del sistema recibe automáticamente un rol "Super Administrador" con **todos** los permisos del catálogo (`BootstrapSuperAdminAsync`, cubierto por pruebas unitarias). Ese primer usuario **no** recibe automáticamente acceso a ninguna empresa, pero puede crear la primera empresa y otorgarse acceso a sí mismo enteramente desde la UI. El segundo usuario en adelante no recibe nada — necesita que el superadmin ya bootstrapeado lo dé de alta vía UI (`/users/[id]`).
- `docs/multi-company.md`, en cambio, **sí está al día**.

Conclusión para el manual: documentar el flujo real ("el primer login bootstrapea un superadministrador; desde ahí todo se hace por UI"), no el flujo manual descrito en los docs viejos.

---

## 1. Objetivo de cada submódulo

**Empresas (Companies)**: catálogo de las entidades legales (hasta 50) que comparten el único tenant de Entra ID. Raíz de la segregación multiempresa.

**Unidades organizativas (OrgUnits)**: árbol jerárquico interno de cada empresa (unidad de negocio, dirección, gerencia, departamento, área, equipo, sucursal, ubicación física, almacén, centro de datos, proyecto).

**Usuarios (Users)**: perfil local de cada principal de Entra ID autenticado. Nunca se crean a mano: se crean solos en el primer login; la pantalla solo administra roles, acceso a empresas y anonimización.

**Roles y Permisos**: RBAC clásico. Roles = paquetes de permisos, globales (no acotados por empresa). Permisos = catálogo fijo, sembrado por migración, de solo lectura vía API en V1.

**Categorías de activos (AssetCategories)**: catálogo CRUD que define tipos de activo y, por categoría, campos técnicos personalizados (texto, número, fecha, booleano o selección) con obligatoriedad configurable.

---

## 2. Modelo de permisos — catálogo completo

Formato `{Módulo}.{Acción}`, sembrado solo por migración; no se crean permisos nuevos desde la API en V1. Solo 5 módulos tienen handlers que realmente los exigen hoy (Companies, Structure, Roles, Permissions, Users) — el resto existe para que fases futuras solo tengan que exigirlo.

| Módulo | Código | Descripción | ¿Enforced hoy? |
|---|---|---|---|
| Companies | Read/Create/Update | Consultar/crear/editar y activar-desactivar empresas | Sí |
| Structure | Read/Create/Update | Estructura organizacional | Sí |
| Roles | Read/Create/Update/Duplicate | Roles | Sí |
| Permissions | Read/Manage | Catálogo y matriz de permisos por rol | Sí |
| Users | Read/Update/ManageRoles/ManageCompanies | Perfil, roles y empresas de usuarios | Sí |
| Assets | Read/Create/Update/Decommission | Activos | Sí |
| Assignments | Read/Create/Update | Asignaciones | Sí |
| Returns | Read/Create | Devoluciones | Sí |
| Loans | Read/Create/Update | Préstamos | Sí |
| Transfers | Read/Create/Update | Transferencias | Sí |
| Movements | Read | Movimientos | Sí |
| Requests | Read/Create/Update | Solicitudes internas | Sí |
| Maintenance | Read/Create/Update | Mantenimientos | Sí |
| Warranties | Read/Create/Update | Garantías | Sí |
| SpareParts | Read/Create/Update | Refacciones | Sí |
| Consumables | Read/Create/Update | Consumibles | Sí |
| Documents | Read/Create | Documentos | Sí |
| Templates | Read/Create/Update | Plantillas | No verificado en este análisis |
| Reports | Read/ReadConsolidated/Export | Reportes | Sí |
| Audit | Read | Auditoría | Sí |
| Catalogs | Read/Create/Update | **Categorías de activos usan este módulo** (no "Assets") | Sí |
| Imports | Read/Create | Lotes de importación | Sí |
| Exports | Create | Exportaciones | Sí |
| Approvals | Read/Configure/Approve/Reject | Flujos y aprobaciones | Sí (Approve/Reject sin uso real) |
| Configuration | Read/Update | Configuración del sistema | Sin handlers vistos |
| Dashboards | ViewExecutive | Dashboard ejecutivo | Sí (migración posterior, aislada) |

**Nota importante**: las categorías de activos usan el permiso `Catalogs.*`, no un permiso `AssetCategories.*` dedicado.

---

## 3. Multiempresa: selección de empresa activa y validación de membresía

**Mecanismo real (`CurrentUserProvisioningMiddleware`):**

1. En cada request autenticado, aprovisiona/actualiza el usuario y guarda en `HttpContext.Items`: `LocalUserId`, `EntraObjectId`, `DisplayName`, `Email`, **`AccessibleCompanyIds`** (todas las `UserCompany` del usuario).
2. Si el request trae `X-Active-Company-Id` y coincide con una membresía real, se guarda como `ActiveCompanyId`; si no coincide, se ignora silenciosamente.
3. `ICurrentCompanyContext.CompanyId` **solo se usa hoy en `GetMeQuery`**. Ningún handler de escritura ni lectura confía en él para filtrar datos.
4. La verdadera frontera de seguridad es `AccessibleCompanyIds`: (a) Global Query Filter de EF Core en la mayoría de entidades transaccionales; (b) cada handler de escritura valida explícitamente `AccessibleCompanyIds.Contains(request.CompanyId)` — mensaje: `"El usuario no tiene acceso a la empresa indicada."`
5. **Confirmado en código y en `docs/multi-company.md`**: *"nada en esta app envía hoy `X-Active-Company-Id`"*. El frontend **nunca envía ese header**. El "selector de empresa activa" en la UI es puramente un `?companyId=` en la URL — no tiene relación con el header backend. `AuditEntry` no lleva filtro por `CompanyId` en absoluto (permiso global tenant-wide).

**Endpoints sensibles a multiempresa**: `POST/GET /org-units` validan `AccessibleCompanyIds`. `Companies`, `Roles`, `Permissions`, `Users` **no** están acotados por empresa.

---

## 4. Pantallas de frontend — campos, validaciones, acciones y estado real

Nota general: **ninguna de estas pantallas usa `next-intl`/`es.json`** — textos literales en `.tsx`. Todas las pantallas están **funcionales de extremo a extremo**.

### 4.1 `/companies` (listado)
Tabla: Nombre comercial, Razón social, RFC/Id. fiscal, Moneda, Zona horaria, Estado, botón Activar/Desactivar. Vacío: "Todavía no hay empresas registradas." Error 403: "No tienes permiso para consultar empresas (Companies.Read)." Botón "Nueva empresa".

### 4.2 `/companies/new`
Campos: Nombre comercial (requerido, máx 200), Razón social (requerido, máx 200), RFC/Id. fiscal (requerido, máx 50), Moneda base ISO 4217 (requerido, exactamente 3 letras, mayúsculas, default MXN), Zona horaria IANA (Select poblado con `Intl.supportedValuesOf("timeZone")`, default America/Mexico_City).
- Validación cliente: "Todos los campos son obligatorios y la moneda debe tener 3 letras (ISO 4217)."
- Dominio: "La razón social es obligatoria.", "El nombre comercial es obligatorio.", "La identificación fiscal es obligatoria.", "La moneda base debe ser un código ISO 4217 de 3 letras.", "La zona horaria es obligatoria."
- Regla: máximo 50 empresas — "No se pueden registrar más de 50 empresas." (409)
- Botón "Crear empresa". **Funcional end-to-end.**

### 4.3 `/org-units`
Si `me.companies.length === 0` → EmptyCompanyState: "Todavía no tienes acceso a ninguna empresa. Pide a un administrador que te agregue desde el módulo de usuarios."
Selector de empresa (`CompanySwitcher`, vía `?companyId=`).
Tabla: Nombre, Tipo, Código, Estado, "Mover a" + botón "Mover", botón Activar/Desactivar. Vacío: "Esta empresa todavía no tiene estructura organizacional." Error 403: "No tienes permiso para consultar la estructura organizacional (Structure.Read)."
Alta (`CreateOrgUnitForm`): Nombre (requerido, máx 200), Código (requerido, máx 50), Tipo (Select obligatorio), Unidad padre (opcional, default "Ninguna (raíz)").
- Validación cliente: "Nombre, código y tipo son obligatorios."
- Reglas: tipo debe existir y estar activo (404); padre debe existir en la misma empresa (404).
- Mover: valida misma empresa (409: "La nueva unidad padre debe pertenecer a la misma empresa."), sin ciclos (409: "El movimiento crearía un ciclo..."), guard de profundidad (409: "La jerarquía organizacional es demasiado profunda para validarse.").
- Dominio: "El nombre de la unidad organizacional es obligatorio.", "El código de la unidad organizacional es obligatorio.", "Una unidad organizacional no puede ser su propio padre."
- **Funcional end-to-end.** No hay edición de nombre/código de una OrgUnit existente — solo alta, mover, activar/desactivar.

### 4.4 `/roles` (listado)
Tabla: Nombre (link), Descripción, Permisos (conteo), Estado. Subtítulo: "Los permisos son globales — la empresa no acota qué puede hacer un rol." Vacío: "Todavía no hay roles." Error 403: "No tienes permiso para consultar roles (Roles.Read)."

### 4.5 `/roles/new`
Campos: Nombre (requerido, máx 100), Descripción (opcional, máx 500). Validación cliente: "El nombre es obligatorio." Dominio: "El nombre del rol es obligatorio."

### 4.6 `/roles/[id]` — tres formularios independientes
- **Renombrar**: Nombre, Descripción → `PUT /roles/{id}`. Éxito: "Guardado."
- **Duplicar**: campo "Duplicar como" → `POST /roles/{sourceId}/duplicate`, permiso `Roles.Duplicate`. Copia matriz de permisos completa. Validación: "Escribe un nombre para el rol duplicado."
- **Matriz de permisos**: checkboxes agrupados por módulo → `PUT /roles/{id}/permissions` (reemplaza toda la matriz). Permiso `Permissions.Manage`. Éxito: "Permisos guardados."
- Activar/Desactivar rol: `PATCH /roles/{id}/active`.
- **Funcional end-to-end**, incluidas duplicar, matriz de permisos, activar/desactivar.

### 4.7 `/users` (listado)
Tabla: Nombre (link), Correo, Estado, Último acceso. Vacío: "Todavía no hay usuarios (se crean automáticamente en el primer inicio de sesión)." Sin botón "Nuevo usuario" (correctamente).

### 4.8 `/users/[id]`
- **Roles asignados**: lista + "Quitar" (`DELETE /users/{id}/roles/{roleId}`); Select + "Asignar" (`POST .../roles/{roleId}`), solo roles activos que el usuario aún no tiene. Permiso `Users.ManageRoles`.
- **Empresas con acceso**: análogo, "Revocar"/"Otorgar", permiso `Users.ManageCompanies`.
- **Privacidad**: botón destructivo "Anonimizar (irreversible)" (`POST /users/{id}/anonymize`, permiso `Users.Update`, sin confirmación modal). Texto: "Elimina permanentemente el nombre y correo reales de este perfil... No afecta el historial de auditoría ya registrado. Esta acción no se puede deshacer." Si ya anonimizado: "Anonimizado el {fecha}. El nombre y correo reales ya no están disponibles."
- **Funcional end-to-end.** No hay edición de DisplayName/Email manual.

### 4.9 `/asset-categories` (listado)
Tabla: Nombre (link), Código, Tecnología por defecto, Campos personalizados (conteo), Estado. Error 403: "No tienes permiso para consultar categorías (Catalogs.Read)."

### 4.10 `/asset-categories/new`
Campos: Nombre (requerido, máx 100), Código (requerido, máx 50), Tecnología de identificación por defecto (Select: QR/Barras/QR y barras/NFC/RFID, default Qr). Validación cliente: "Nombre y código son obligatorios." Unicidad: 409 "Ya existe una categoría con el código '{código}'."

### 4.11 `/asset-categories/[id]`
Encabezado + Activar/Desactivar. Tabla de campos técnicos: Nombre, Código, Tipo, Obligatorio, Opciones.
Formulario "Agregar campo técnico": Nombre (requerido, máx 100), Código (requerido, máx 50), Tipo de dato (Texto/Número/Fecha/Sí-No/Selección), checkbox Obligatorio, Opciones (si Selección).
- Validación cliente: "Nombre y código son obligatorios." y "Un campo de selección necesita al menos una opción."
- Dominio: mismos mensajes + unicidad de código dentro de la categoría (nota: esta excepción es `DomainException` → 422, no `ConflictException` → 409, inconsistente con `AssetCategory.Code`).
- No hay edición ni borrado de un campo técnico ya creado. **Funcional end-to-end.**

---

## 5. Endpoints REST completos

Todos los controladores llevan `[Authorize]` a nivel de clase; la autorización real por permiso ocurre en `AuthorizationBehavior` (MediatR), no por atributos ASP.NET.

### CompaniesController — `api/v1/companies`
GET `/` (Companies.Read), GET `/{id}` (Companies.Read), POST `/` (Companies.Create, 201, límite 50 empresas), PATCH `/{id}/active` (Companies.Update).
*No existe `PUT /companies/{id}` para editar el perfil de una empresa ya creada, pese a que `Company.UpdateProfile()` existe en el dominio sin usarse desde ningún handler — código muerto/incompleto.*

### OrgUnitsController — `api/v1/org-units`
GET `/types`, GET `/?companyId=`, POST `/` (Structure.Create), PATCH `/{id}/move` (Structure.Update), PATCH `/{id}/active` (Structure.Update).

### RolesController — `api/v1/roles`
GET `/`, GET `/{id}`, POST `/` (Roles.Create), POST `/{sourceId}/duplicate` (Roles.Duplicate), PUT `/{id}` (Roles.Update), PATCH `/{id}/active` (Roles.Update), PUT `/{id}/permissions` (Permissions.Manage).

### PermissionsController — `api/v1/permissions`
GET `/` (Permissions.Read) — único endpoint, solo lectura.

### UsersController — `api/v1/users`
GET `/`, GET `/{id}`, POST/DELETE `/{id}/roles/{roleId}` (Users.ManageRoles), POST/DELETE `/{id}/companies/{companyId}` (Users.ManageCompanies), POST `/{id}/anonymize` (Users.Update).
*Nota: `AssignRoleToUserCommand`/`GrantUserCompanyAccessCommand` son auditables; `RemoveRoleFromUserCommand`/`RevokeUserCompanyAccessCommand` (quitar) NO lo son — asimetría.*

### AssetCategoriesController — `api/v1/asset-categories`
GET `/`, GET `/{id}`, POST `/` (Catalogs.Create, 409 código duplicado), POST `/{id}/custom-fields` (Catalogs.Update), PATCH `/{id}/active` (Catalogs.Update).

### MeController — `api/v1/me`
GET `/` — **sin política de permiso**, solo autenticación. Devuelve perfil, permisos, empresas, empresa activa.

Mapeo de excepciones global: ValidationException→400, ForbiddenAccessException→403, NotFoundException→404, ConflictException/DbUpdateConcurrencyException→409, DomainException→422, otro→500. Todas incluyen `correlationId`.

---

## 6. Reglas de negocio no obvias

1. **`CompanyId` de un `Asset` nunca se edita directo** — solo vía `Transfer` completada.
2. **Unicidad en cascada con distinto alcance**: `Company.TaxId` único globalmente (sin verificación explícita en handler — riesgo); `OrgUnitType.Code` único globalmente; `OrgUnit` único por (CompanyId, Code) sin verificación explícita en handler; `AssetCategory.Code` único globalmente (sí verificado); `CustomFieldDefinition.Code` único dentro de su categoría (DomainException, 422); `Permission (Module, Action)` único globalmente.
3. **Jerarquía de OrgUnits — anti-ciclo verificado a nivel de aplicación**, no de dominio (`MoveOrgUnitCommandHandler.EnsureNoCycleAsync`, guard de 1000 saltos).
4. **Roles y permisos nunca se borran físicamente**, solo se desactivan.
5. **Los permisos son estrictamente globales** — nunca escopeados por empresa.
6. **Anonimización de usuario es irreversible y distinta de desactivación.** No toca `AuditEntry.UserDisplayName` ya escrito (denormalizado a propósito).
7. **Bootstrap del primer superadministrador** — excepción de seguridad deliberada y auditada (`BootstrapSuperAdmin`).
8. **`CustomFieldDataType.Select` exige `Options`** — doble verificación (cliente y dominio).
9. **`Reports.ReadConsolidated` como permiso separado** — no toda persona con acceso a una empresa debe ver el consolidado.

---

## 7. Mensajes de error/éxito relevantes

**Dominio (422):**
"La razón social es obligatoria." / "El nombre comercial es obligatorio." / "La identificación fiscal es obligatoria." / "La moneda base debe ser un código ISO 4217 de 3 letras." / "La zona horaria es obligatoria." / "El nombre de la unidad organizacional es obligatorio." / "El código de la unidad organizacional es obligatorio." / "Una unidad organizacional no puede ser su propio padre." / "El nombre del rol es obligatorio." / "El identificador de Entra ID es obligatorio para crear el perfil local." / "El nombre del usuario es obligatorio." / "El correo del usuario es obligatorio." / "El usuario ya fue anonimizado." / "El nombre de la categoría es obligatorio." / "El código de la categoría es obligatorio." / "Ya existe un campo personalizado con el código '{código}' en esta categoría." / "El nombre del campo personalizado es obligatorio." / "El código del campo personalizado es obligatorio." / "Un campo de selección debe definir sus opciones."

**Aplicación (403/409/404):**
"El usuario no tiene acceso a la empresa indicada." / "No se pueden registrar más de 50 empresas." / "La nueva unidad padre debe pertenecer a la misma empresa." / "El movimiento crearía un ciclo..." / "La jerarquía organizacional es demasiado profunda para validarse." / "Ya existe una categoría con el código '{código}'." / "Uno o más identificadores de permiso no existen en el catálogo." / "La cuenta de usuario está desactivada." (texto plano, no ProblemDetails)

**UI (hardcodeados):**
"No tienes permiso para consultar empresas/roles/usuarios/la estructura organizacional/categorías (...)." / "Todavía no hay empresas registradas./roles./usuarios (se crean automáticamente...)." / "Esta empresa todavía no tiene estructura organizacional." / "Esta categoría todavía no tiene campos técnicos." / "Todavía no tienes acceso a ninguna empresa. Pide a un administrador que te agregue desde el módulo de usuarios." / "Guardado." / "Permisos guardados." / textos de anonimización citados arriba.

---

## 8. Casos especiales / edge cases / ambigüedades

1. Docs desactualizados vs. código (§0).
2. `Company.UpdateProfile()` existe en dominio pero sin comando/endpoint/UI expuesto — no se puede editar una empresa ya creada.
3. Falta verificación explícita de unicidad para `Company.TaxId` y `OrgUnit.Code` en sus handlers — probable 500 genérico en vez de 409 amigable ante colisión.
4. Asimetría de auditoría en `UsersController`: asignar/otorgar se audita; quitar/revocar no.
5. `CustomFieldDefinition.Code` duplicado → 422; `AssetCategory.Code` duplicado → 409 — inconsistencia de status HTTP para el mismo tipo de error conceptual.
6. Migración `AddDashboardsPermission` agrega un permiso aislado mucho después — el catálogo puede crecer entre versiones vía migración.
7. El menú lateral muestra siempre todas las opciones de "Administración" sin filtrar por permiso — control real solo al cargar cada página (403 reactivo).
8. Ningún texto de estas pantallas pasa por `next-intl`/`es.json`, pese al lineamiento de CLAUDE.md.
9. `GetMeQuery` es la única consulta sin `IRequiresPermission` — deliberado y documentado.
10. El primer superadmin no recibe compañías automáticamente — secuencia real: 1) login inicial, 2) `/companies` crear primera empresa, 3) `/users` buscarse y otorgarse esa empresa, 4) recién ahí `/org-units`, `/assets`, etc. quedan operables.
