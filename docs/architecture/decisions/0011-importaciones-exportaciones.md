# ADR 0011 — Importaciones/Exportaciones: alcance V1, formatos y el descubrimiento del filtro de empresa en el worker

## Estado
Aceptado (F9, 2026-09-18).

## Contexto
ADR 0003 (F0) ya había resuelto por adelantado la tensión central de F9: un lote de hasta 10,000 filas no
cabe en un solo request HTTP síncrono con validación completa, así que el flujo debía ser asíncrono y
desacoplado (`ImportBatch` + `IImportQueue`, `Channel<T>`+`BackgroundService` en local, Azure Storage Queue
en la nube). F9 construyó todo lo que ADR 0003 dejó pendiente: el agregado en sí, el worker real, la
validación/confirmación en dos fases, y la exportación — que resultó no necesitar nada de eso.

## Decisiones

### 1. Alcance V1: solo `Asset`
Igual que Documentos limitó su allowlist a `Asset`/`MaintenanceOrder` en F8, importar/exportar se limita en
V1 a activos — es la entidad con el catálogo de campos más rico y el caso de uso real que el pedido describe
("carga inicial del inventario"). Extender a otras entidades es agregar un nuevo `RowValidator`/proyección
de exportación, no un cambio estructural.

### 2. CSV para importar, Excel/PDF para exportar
Formatos distintos por propósito: importar es datos estructurados (una hoja de cálculo guardada como CSV,
o exportada de otro sistema), exportar es para lectura/compartir humano. Se evita la ambigüedad de manejar
múltiples hojas o fórmulas en un `.xlsx` de entrada — CsvHelper resuelve el parseo de forma robusta
(comillas, campos con comas) sin inventar un parser propio.

### 3. Validación en dos fases con confirmación explícita
Cumple "vista previa" + "modo transaccional completo o solo-filas-válidas" del pedido literalmente: subir
el archivo dispara solo la fase de validación (`Queued → Validating → Validated`, con un reporte
fila-por-fila) — nada se escribe todavía. El usuario revisa el reporte y confirma con
`POST /import-batches/{id}/commit` eligiendo `AllOrNothing` o `ValidRowsOnly`
(`Validated → Processing → Completed/CompletedWithErrors/Failed`). La confirmación **vuelve a validar**
cada fila en el momento de escribir, no solo confía en el reporte de la vista previa — el estado real
(categoría desactivada, serie duplicada por otra importación concurrente) pudo cambiar entre ambos
momentos, el mismo riesgo que F4/F5/F7 ya aceptaron para aprobaciones. Si el modo es `AllOrNothing` y
alguna fila ya no es válida al confirmar, el lote pasa a `Failed` con el reporte adjunto y no se crea
ningún activo — la propia clase agregada (`ImportBatch.RequestCommit`) además rechaza de entrada un intento
de confirmar en `AllOrNothing` si el reporte de la vista previa ya mostraba filas inválidas, así que la
mayoría de esos casos ni siquiera llegan a encolarse (se devuelve un 422 inmediato al usuario).

### 4. Detección de duplicados por número de serie
Dentro de la misma empresa: contra activos ya existentes y contra otras filas del propio lote. No hay un
identificador externo distinto en el pedido para esta comparación — el número de serie es el único campo
con vocación de identificador natural que ya existe en `Asset`.

### 5. El worker no reinvoca `CreateAssetCommand` vía `ISender`
`CreateAssetCommandHandler` depende de `ICurrentCompanyContext`/`ICurrentUserContext`, resueltos de
`HttpContext` — inexistentes dentro de un `BackgroundService`. El lote ya trae su propio `CompanyId` y
`CreatedByUserId` (capturados al subir el archivo); `ImportBatchProcessor` llama directamente los mismos
métodos de dominio (`Asset.Create`, `IssueTag`, `SetCustomFieldValues`) y el mismo `IFolioGenerator` que el
handler usa — mismo criterio exacto que `InternalRequestApprovalReactionHandler` (F7, ADR 0009) ya
estableció: no duplicar reglas de negocio (viven en el agregado), solo evitar el acoplamiento a HTTP.

### 6. `IImportQueue` expone encolar y leer con el mismo contrato
`EnqueueAsync` + `DequeueAllAsync` — el único `ImportBatchBackgroundService` funciona sin cambios con
cualquier implementación (`ChannelImportQueue` local, `AzureStorageQueueImportQueue` en producción),
cumpliendo literalmente lo que ADR 0003 ya anticipaba ("mismo contrato funciona en local y en Azure"). El
worker es idempotente por diseño: al recibir un `ImportBatchId` solo mira el `Status` ya persistido y
decide el siguiente paso (`Queued` → valida, `Processing` → confirma); cualquier otro estado —incluida una
entrega repetida del mismo mensaje— es un no-op.

### 7. Exportar es síncrono y streameado, no pasa por `ImportBatch`
Es de solo lectura, sin la escala de escritura fila-a-fila que justificó la cola al importar — reutiliza
los mismos filtros que `GetAssetsQuery` (F2), sin paginar (tope de 10,000 filas, documentado, mismo tope
que importar). Se genera el archivo completo en memoria y se sirve streameado por la API, mismo patrón que
la descarga de `Document` (F8).

### 8. CsvHelper/ClosedXML/PdfSharp se referencian directamente en `Application`
Son librerías de cómputo puro en memoria (parseo/serialización, sin red ni recurso externo) — mismo
criterio que ya aplica a FluentValidation/MediatR/EF Core, referenciadas directamente en `Application`
desde F0. Distinto de `IFileStorage`/`IEmailSender` (F8), que sí envuelven un recurso externo real (una
cuenta de Azure Storage, un servidor SMTP) y por eso viven detrás de un puerto en Infraestructura.

**PdfSharp se eligió sobre QuestPDF por licencia** — MIT sin condición de ingresos, evitando la ambigüedad
de la licencia Community de QuestPDF (gratuita solo bajo ~1M USD de ingresos anuales) en una aplicación de
un tercero cuyos ingresos no son verificables desde aquí. El costo real de esa elección: PdfSharp 6 no
tiene resolutor de fuentes de plataforma dentro del contenedor Linux de este despliegue (confirmado contra
la documentación/foro oficial de PDFsharp) — sin una fuente explícita, cualquier `XFont` lanza una
excepción al primer intento de dibujar texto. Se resolvió empaquetando **Liberation Sans** (SIL Open Font
License 1.1, métricamente compatible con Arial, license de redistribución libre — ver
`ImportExport/Fonts/LICENSE-LiberationFonts.txt`) como recurso incrustado en `Application`, con un
`EmbeddedFontResolver` (`IFontResolver`) que la sirve en tiempo de ejecución — así el PDF nunca depende de
qué fuentes tenga instaladas el host.

### 9. Sin auto-refresco en la UI
El usuario recarga manualmente `/imports/{id}` para ver el progreso — mismo criterio que el resto de la
app: ningún flujo asíncrono existente hasta ahora (aprobaciones incluidas) usa *polling* ni WebSockets.
Documentado como simplificación V1, no como carencia — agregar *polling* ahí sería la primera instancia de
ese patrón en el proyecto y no estaba pedido explícitamente.

### 10. Descubrimiento real durante la construcción: el filtro de empresa en el worker
`Asset`, `OrgUnit` e `ImportBatch` llevan un filtro de consulta global basado en
`ICurrentCompanyContext.AccessibleCompanyIds`, que a su vez se resuelve de `HttpContext.Items` (ver
`HttpContextCurrentCompanyContext`). **F9 es el primer `BackgroundService` de este proyecto que necesita
leer datos con ese filtro fuera de un request HTTP real** — dentro del *scope* de DI del worker no existe
`HttpContext`, así que `AccessibleCompanyIds` evalúa vacío, y el filtro de consulta habría devuelto cero
filas silenciosamente para cualquier `SELECT` sobre esas tablas (no para los `INSERT` de los activos
nuevos, que no pasan por el filtro — solo para las lecturas: detección de duplicados de serie, resolución
de `OrgUnitCode`, y releer el propio `ImportBatch` para decidir el siguiente paso). Se resolvió de forma
consistente con el precedente que ADR 0010 ya fijó ("nunca confiar en un contexto de empresa implícito"):
**toda lectura dentro de `ImportBatchProcessor` usa `IgnoreQueryFilters()` explícitamente y filtra por el
`CompanyId` propio del lote** en su lugar — nunca por la empresa "activa" de un usuario que, en este
contexto, no existe. Documentado con un comentario extenso en la propia clase para que quien agregue un
nuevo `db.Algo.Where(...)` ahí no repita el filtro implícito por accidente.

## Consecuencias
- Extender el allowlist de importación/exportación a otra entidad (Consumibles, por ejemplo) es agregar un
  `RowValidator`/una proyección de exportación análogos — no una limitación estructural.
- `docs/multi-company.md`/ADR 0010 ganan un caso más del mismo patrón: cualquier futuro `BackgroundService`
  que lea entidades con filtro de empresa debe repetir la misma disciplina (`IgnoreQueryFilters()` +
  `CompanyId` explícito), documentado aquí como el primer precedente concreto.
- Verificado por integración contra SQL Server y el propio `ImportBatchBackgroundService` real corriendo
  en el host de pruebas (no un *fake* del worker): validar reporta filas válidas/inválidas correctamente,
  confirmar en `ValidRowsOnly` crea solo los activos válidos, confirmar en `AllOrNothing` con filas
  inválidas se rechaza sin crear nada, cancelar antes de confirmar no crea nada, y exportar a Excel/PDF
  produce archivos válidos y legibles.
