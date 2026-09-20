# ADR 0012 — Reportes y paneles: alcance V1, consolidado vs por empresa, y dos bugs reales de traducción SQL

## Estado
Aceptado (F10, 2026-09-19).

## Contexto
El roadmap describe F10 como "Paneles operativos, KPIs (MTTR/MTBF, vencimientos, existencias bajas)". A
diferencia de toda fase anterior, F10 **no agregó ningún agregado ni tabla nueva** — es el contexto
"Reporting" que el análisis inicial ya describía como "proyecciones/consultas de solo lectura para
paneles y exportaciones, consume los demás contextos vía consultas, nunca escribe". Todos los datos que
pide el roadmap ya existían: `MaintenanceOrder.OpenedAtUtc/ClosedAtUtc` (MTTR/MTBF), `Warranty.EndDate`
(vencimientos), `Consumable.MinimumStock/CurrentStock` (existencias bajas — `MinimumStock` existe desde F6
y nunca se había usado), `Asset.Status`/`AssetCategoryId` (inventario). Sin migración en esta fase.

## Decisiones

### 1. `Reports.Read` vs `Reports.ReadConsolidated` según la forma del propio request
Cada query trae `Guid? CompanyId`: si se envía, exige `Reports.Read` y valida membresía a esa empresa
(igual que el resto de la app); si se omite, exige `Reports.ReadConsolidated` y agrega sobre **todas** las
empresas accesibles del usuario. `IRequiresPermission.PermissionCode` ya es una propiedad computada en
C# — no hizo falta enseñarle nada nuevo a `AuthorizationBehavior`, solo condicionarla al propio `CompanyId`
del request (`ReportScope.PermissionCode`). Mismo espíritu que `GetAuditEntriesQuery.CompanyId` opcional
(F8, ADR 0010).

### 2. Cuatro paneles V1, los que el roadmap nombra explícitamente
- **Resumen de inventario**: conteo de `Asset` por `Status` y por `AssetCategoryId`. Sin exportación — es
  orientación, no una lista de trabajo.
- **MTTR/MTBF**: MTTR = promedio de `ClosedAtUtc - OpenedAtUtc` de órdenes `Closed` (horas). MTBF =
  promedio agrupado de los intervalos entre `OpenedAtUtc` consecutivos de un mismo activo con 2+ órdenes
  (días) — calculado en memoria sobre las órdenes ya materializadas, mismo criterio de volumen que
  `GetMyPendingApprovalsQuery` (ADR 0006) ya acepta para V1. Desglose por categoría además del total.
- **Garantías por vencer**: `Warranty` con `EndDate` dentro de una ventana configurable (`withinDays`,
  por defecto 30) **o ya vencidas** — ambos casos son accionables y se muestran juntos, ordenados por
  `EndDate`.
- **Existencias bajas**: `Consumable` con `MinimumStock` definido y `CurrentStock <= MinimumStock`,
  ordenado por el faltante descendente.
Los tres últimos son listas de trabajo exportables (`Reports.Export`); el resumen de inventario no.

### 3. Exportar reutiliza la infraestructura de F9, no la duplica
Se extrajo el `BuildXlsx`/`BuildPdf` que vivía privado dentro de `ExportAssetsQueryHandler` (F9) a una
clase compartida `Application/Common/TabularFileBuilder.cs` (mismo ClosedXML/PdfSharp/
`EmbeddedFontResolver`, cero comportamiento nuevo) — tanto `ExportAssetsQuery` (F9, adaptado para
consumirla) como los tres `Export*Query` de F10 la llaman. Las 31 pruebas de F9 relacionadas con
exportación pasaron sin modificarse tras el refactor, confirmando que no hubo cambio de comportamiento.

### 4. Sin caché ni materialización periódica
Cada consulta agrega en el momento sobre tablas ya indexadas por `CompanyId`/fecha. A la escala de V1 (una
empresa o el consolidado de un tenant) no se justifica una tabla de proyección separada — punto de
extensión futuro si el volumen real lo exige.

### 5. Dos bugs reales de traducción SQL Server, encontrados y corregidos durante la construcción
Ambos aparecieron solo contra SQL Server real (las pruebas unitarias con el proveedor InMemory de EF Core
no los reprodujeron — mismo patrón exacto que el bug de F3 documentado en `roadmap.md`):

- **`GetLowStockConsumablesQuery`**: proyectar `c.MinimumStock!.Value` (operador de indulgencia nula
  seguido de `.Value`) dentro de un `Select` no se traduce en el proveedor de SQL Server aunque el `Where`
  previo ya garantizara that el valor no es nulo. Se corrigió con `c.MinimumStock ?? 0` (se traduce a
  `ISNULL`/`COALESCE`) — sigue siendo seguro exactamente por el mismo filtro. Además, ordenar
  (`OrderByDescending`) por una propiedad calculada de un registro ya proyectado falló por separado; se
  corrigió ordenando **antes** de proyectar al record final, sobre la expresión cruda
  `(c.MinimumStock ?? 0) - c.CurrentStock`.
- **`GetMaintenanceKpisQuery`**: proyectar `o.Status == MaintenanceOrderStatus.Closed` (una comparación
  booleana sobre un enum mapeado a `nvarchar` vía `HasConversion<string>()`) dentro de un `Select` lanza
  `SqlException: The data types nvarchar and nvarchar are incompatible in the '^' operator` — **la misma
  causa raíz exacta** que el bug ya documentado en F3 (`GetMyAssignmentsQuery`, ver `roadmap.md`, "Estado
  de F3"), solo que esta vez dentro de una proyección en lugar de un `OrderBy`. Se corrigió con el mismo
  criterio: seleccionar el enum crudo (`o.Status`) y comparar en memoria, nunca dentro de la traducción a
  SQL.

Ambos confirman, por segunda vez en este proyecto, que una comparación booleana sobre un enum mapeado a
`nvarchar` (o un `.Value` sobre un nulable ya filtrado) es una traducción a evitar sistemáticamente en
cualquier query nueva sobre columnas con `HasConversion<string>()`/nulables — documentado aquí para que la
próxima fase lo recuerde sin tener que redescubrirlo.

## Consecuencias
- Verificado por integración contra SQL Server real: reportes por empresa aíslan correctamente los datos
  de cada una, el modo consolidado agrega dos empresas reales, y las tres exportaciones (Excel/PDF)
  producen archivos válidos y legibles.
- `TabularFileBuilder` queda disponible para cualquier exportación tabular futura (F11 búsqueda avanzada,
  u otra), sin repetir la lógica de paginación de PDF ni el resolutor de fuentes.
