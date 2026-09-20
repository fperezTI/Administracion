# ADR 0013 — Búsqueda avanzada: alcance multi-entidad y autorización por tipo de resultado

## Estado
Aceptado (F11, 2026-09-20).

## Contexto
El roadmap describe F11 como "Búsqueda global multi-entidad con permisos". A diferencia de casi todas las
fases anteriores, no existía ningún bounded context "Search" reservado en el análisis inicial (§5 solo
listaba "Reporting"/"Import/Export" como transversales) ni ningún permiso `Search.*` sembrado — el diseño
quedó completamente abierto, como ya pasó con F10 (Reportes).

## Decisiones

### 1. Sin agregado ni tabla nueva
Mismo espíritu que F10: `GlobalSearchQuery` consulta directamente las tablas ya existentes, nunca escribe.
Sin migración en esta fase.

### 2. Autorización por tipo de resultado, no por el request completo
`GlobalSearchQuery` no implementa `IRequiresPermission` — igual que `GetMeQuery`, pasa por
`AuthorizationBehavior` sin bloquear (cualquier usuario autenticado puede buscar). El handler inyecta
`IPermissionChecker` directamente y llama `HasPermissionAsync(userId, "{Módulo}.Read", ct)` una vez por
cada uno de los 8 tipos buscables antes de incluirlo en los resultados. Un usuario sin `Warranties.Read`,
por ejemplo, simplemente nunca ve resultados de garantías — la búsqueda entera nunca falla con 403 por
faltarle un permiso de un solo tipo. Es el primer lugar del proyecto donde `IPermissionChecker` se usa
imperativamente fuera del pipeline de MediatR, un patrón ya disponible desde F1 pero sin precedente de uso
hasta ahora.

### 3. Ocho tipos buscables en V1, sin permiso nuevo que inventar
Cada tipo de entidad ya tenía su propio permiso `{Módulo}.Read` desde F1 — reutilizarlos evita el problema
que Reportes sí tuvo que resolver (F10, ADR 0012, `Reports.Read`/`ReadConsolidated`): aquí no hace falta
una categoría de permiso nueva, solo comprobar la ya existente por cada tipo.

| Entidad | Campos de texto | Permiso | Enlaza a |
|---|---|---|---|
| `Asset` | Folio, marca, modelo, serie | `Assets.Read` | `/assets/{id}` |
| `Movement` | Folio | `Movements.Read` | `/assets/{AssetId}` (ver decisión 4) |
| `MaintenanceOrder` | Folio, descripción | `Maintenance.Read` | `/maintenance-orders/{id}` |
| `Warranty` | Proveedor | `Warranties.Read` | `/warranties/{id}` |
| `SparePart` | Nombre, número de parte, serie | `SpareParts.Read` | `/spare-parts/{id}` |
| `Consumable` | Nombre, SKU | `Consumables.Read` | `/consumables/{id}` |
| `InternalRequest` | Justificación | `Requests.Read` | `/requests/{id}` |
| `User` | Nombre, correo | `Users.Read` | `/users/{id}` |

**Quedan fuera de V1, deliberadamente**: `Document` (no tiene página de detalle propia — se muestra dentro
de la ficha de `Asset`/`MaintenanceOrder` vía `DocumentsPanel`, F8; buscar por la entidad asociada ya lo
encuentra indirectamente) y los catálogos administrativos (`Company`, `OrgUnit`, `Role`, `AssetCategory`,
`ApprovalFlowDefinition`, `Template`) — son listas ya pequeñas y enumerables con su propia pantalla, no el
tipo de dato operativo que "búsqueda global" pide encontrar rápido.

### 4. `Movement` enlaza a su `Asset`, no a sí mismo
`/movements` es solo un listado — no existe `/movements/{id}`. Cada `SearchResultItem` separa
`EntityType`/`EntityId` (qué se encontró) de `LinkEntityType`/`LinkEntityId` (adónde navegar); para los
otros 7 tipos ambos pares son iguales, solo `Movement` difiere. El frontend resuelve un único mapeo
`LinkEntityType → ruta`, nunca un caso especial por tipo de resultado.

### 5. `CompanyId` opcional sin una segunda categoría de permiso
A diferencia de F10, aquí no hay distinción `Read`/`ReadConsolidated` sembrada: `CompanyId` ausente busca
en todas las empresas accesibles del usuario (mismo criterio de membresía de siempre, sin permiso
adicional); presente acota a una, validando membresía igual que el resto de la app. `User` es la única
entidad sin `CompanyId` propio — se busca sin ese filtro, coincidiendo con que `/users` tampoco está
acotado por empresa.

### 6. UI sin *type-ahead*
Un formulario GET simple (`<input name="term">`) en `AppHeader`, sin JavaScript, envía a `/search`. Ningún
flujo de esta app usa *polling* ni actualización en vivo todavía (ADR 0011 ya documentó esa misma
abstención para el progreso de importaciones) — un cuadro de texto con búsqueda en vivo habría sido el
primer patrón de ese tipo en el proyecto, sin que el pedido lo exigiera explícitamente.

### 7. Ninguna proyección compara ni ordena por un enum mapeado a texto dentro de la traducción a SQL
`GetMaintenanceKpisQuery`/`GetLowStockConsumablesQuery` (F10, ADR 0012) ya dejaron documentado que
`HasConversion<string>()` sobre un enum, combinado con una comparación (`==`) o un `.Value` sobre un
nulable dentro de un `Select`/`OrderBy` traducido a SQL Server, falla en tiempo de ejecución aunque el
proveedor InMemory de las pruebas unitarias no lo reproduzca. `GlobalSearchQueryHandler` (p. ej. al
formatear `Movement.Type` para el subtítulo) solo selecciona el enum crudo dentro de la consulta y lo
formatea **después** de materializar en memoria — ninguna de las 3 pruebas de integración de F11 tuvo que
corregir nada de esto porque se evitó desde el diseño, no porque se haya vuelto a descubrir.

## Consecuencias
- Verificado por integración contra SQL Server real: un activo aparece por folio parcial, una garantía
  aparece solo cuando el usuario tiene `Warranties.Read`, y acotar por `companyId` excluye resultados de
  otra empresa (el modo sin acotar los incluye a ambos).
- Agregar un noveno tipo buscable (p. ej. `Document` con su padre resuelto) es una sub-consulta más
  siguiendo el mismo patrón — no un cambio estructural.
