# Autorización — RBAC

## Modelo

```
User ──< UserRole >── Role ──< RolePermission >── Permission
User ──< UserCompany >── Company   (membresía; define a qué empresas puede operar)
```

- Un usuario puede tener **varios roles**.
- Un permiso tiene forma `{Module}.{Action}`, por ejemplo: `Assets.Create`, `Assets.Read`, `Assets.Update`,
  `Assets.Decommission`, `Approvals.Approve`, `Audit.Read`, `Roles.Manage`, `Users.Manage`.
- Los permisos son **globales**: no existe una tabla `RolePermissionCompany`. La empresa activa es
  contexto/filtro de datos, no una frontera de autorización (requisito explícito de la sección 6 del pedido).
- Roles y permisos con historial de asignación **no se eliminan físicamente**, solo se desactivan
  (`IsActive = false`).
- Todo cambio a roles, permisos o asignaciones se audita (`Audit` context).

## Módulos con permisos granulares (mínimo, sección 6 del pedido)

Activos, Asignaciones, Devoluciones, Préstamos, Transferencias, Movimientos, Solicitudes, Mantenimientos,
Garantías, Refacciones, Consumibles, Usuarios, Roles, Permisos, Documentos, Plantillas, Reportes, Auditoría,
Empresas, Estructura organizacional, Catálogos, Importaciones, Exportaciones, Flujos de aprobación,
Configuración. Cada módulo define al menos `Read`, `Create`, `Update`, y acciones específicas de dominio
(p. ej. `Assets.Decommission`, `Approvals.Approve`, `Requests.Cancel`).

## Validación en tres capas (defensa en profundidad)

1. **UI**: los componentes de Next.js ocultan/deshabilitan acciones según los permisos del usuario
   (obtenidos del API tras login) — únicamente cosmético, nunca la única barrera. **(Pendiente, requiere
   frontend de F1.)**
2. **API**: `[Authorize]` a nivel de controlador exige autenticación (401 si falta); no se registran ~90
   políticas de ASP.NET Core una por permiso.
3. **[Implementado, F1] Application**: `AuthorizationBehavior<TRequest, TResponse>`
   (`Application/Common/Behaviors/AuthorizationBehavior.cs`), un `IPipelineBehavior` de MediatR, revalida el
   permiso declarado por cada comando/consulta que implementa `IRequiresPermission` (una propiedad
   `PermissionCode`, p. ej. `PermissionCatalog.Companies.Create`) contra `IPermissionChecker`
   (`Infrastructure/Security/EfPermissionChecker.cs`, una consulta EF Core sobre
   `UserRole → Role (activo) → RolePermission → Permission`). Esta es la capa que realmente autoriza — es la
   que se ejerce sin importar la vía de entrada (controlador, futuro job, etc.), y la única con pruebas de
   integración que verifican 401 sin sesión / 403 sin permiso / 200 con permiso concedido
   (`AuthenticationAndRbacTests`).

Las reglas de dominio que no son de autorización pero sí de integridad (p. ej. "un aprobador no puede
aprobar su propia solicitud") se validan **además** en el dominio/aplicación, independientemente del RBAC —
ver `docs/architecture/domain-model.md`.

## Empresa activa y alcance de datos

La empresa activa se deriva del **contexto de sesión del usuario autenticado** (sus membresías `UserCompany`)
combinada con una selección explícita almacenada del lado servidor, nunca de un parámetro de request sin
verificar. Todo repositorio EF Core aplica un **Global Query Filter** por `CompanyId` como defensa adicional
sobre la validación explícita en cada *handler* de aplicación. Detalle en `docs/multi-company.md`.

## Matriz de permisos administrable

La matriz rol↔permiso se administra desde la interfaz (módulo Roles — pendiente de frontend), respaldada por
`PUT /api/v1/roles/{roleId}/permissions` protegido por `Permissions.Manage`. Los 68 permisos base (24
módulos: Companies, Structure, Roles, Permissions, Users, Assets, Assignments, Returns, Loans, Transfers,
Movements, Requests, Maintenance, Warranties, SpareParts, Consumables, Documents, Templates, Reports, Audit,
Catalogs, Imports, Exports, Approvals, Configuration) se siembran por migración
(`PermissionConfiguration.HasData`, con GUID determinístico por código — ver
`Infrastructure/Persistence/Seed/DeterministicGuid.cs`) desde el catálogo único
`Application/Common/Security/PermissionCatalog.cs`; no se crean permisos arbitrarios desde la API en V1
(evita fragmentación de la matriz). Solo Companies, Structure, Roles, Permissions y Users tienen handlers que
realmente los exigen hoy — el resto del catálogo existe para que las fases siguientes solo tengan que
*exigir* el permiso correspondiente, no crearlo. Roles sí se crean, editan, duplican (`Roles.Duplicate`
copia la matriz de permisos del rol origen — `Role.Duplicate` en el dominio) y activan/desactivan vía
`RolesController`.
