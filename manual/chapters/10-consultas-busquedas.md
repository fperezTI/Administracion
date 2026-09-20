# 7. Consultas y búsquedas

Cada listado del sistema (Activos, Asignaciones, Préstamos, Movimientos, Órdenes de mantenimiento,
Consumibles, Refacciones, Garantías, Solicitudes, etc.) trae sus propios filtros específicos, ya
descritos en el capítulo correspondiente a cada módulo. Esta sección no repite ese detalle: cubre
únicamente las tres herramientas de consulta que son **transversales** a todo el sistema —
exportaciones, búsqueda global y reportes — y que no pertenecen a ningún módulo de negocio en
particular.

## Búsqueda global (`/search`)

### Cómo funciona

El buscador global está disponible desde el campo de búsqueda de la barra superior, visible en
todas las pantallas internas del sistema (ver captura ![Captura de pantalla](screenshots/050_busqueda.png)). Al escribir
un término y presionar el botón **"Buscar"**, el sistema consulta simultáneamente ocho tipos de
entidad y agrupa los resultados por tipo en la misma pantalla.

**Requisitos**:

- Requiere sesión activa. Sin sesión válida, el sistema responde **"Se requiere iniciar sesión
  para buscar."**
- El término debe tener **al menos 2 caracteres**. Con menos, se muestra el mensaje **"Escribe al
  menos 2 caracteres para buscar."** sin llegar a consultar el API.
- La coincidencia es de tipo "contiene" (subcadena), no de coincidencia exacta ni de inicio de
  palabra.
- Los resultados se acotan siempre a las **empresas a las que la persona usuaria tiene acceso** —
  nunca se buscan registros de empresas ajenas.
- Cada tipo de entidad devuelve **como máximo 8 resultados**, aunque existan más coincidencias.

### Qué entidades busca y qué permisos la filtran

La búsqueda global no tiene un permiso propio: cada uno de los ocho tipos de entidad se filtra
internamente según si la persona usuaria cuenta con el permiso de lectura de ese módulo. Si no
tiene el permiso de un módulo, ese tipo de entidad simplemente no aparece entre los resultados
(no genera un error visible, se omite en silencio).

| Entidad | Permiso requerido | Campos donde busca el término |
|---|---|---|
| Activo | `Assets.Read` | Folio, Marca, Modelo, Serie |
| Movimiento | `Movements.Read` | Folio |
| Orden de mantenimiento | `Maintenance.Read` | Folio, Descripción |
| Garantía | `Warranties.Read` | Proveedor |
| Refacción | `SpareParts.Read` | Nombre, Serie, Número de parte |
| Consumible | `Consumables.Read` | Nombre, SKU |
| Solicitud interna | `Requests.Read` | Justificación |
| Usuario | `Users.Read` | Nombre, Correo (búsqueda global, sin filtrar por empresa) |

> **Nota**: los resultados de tipo Movimiento no tienen pantalla propia de detalle; al
> seleccionarlos, el sistema lo lleva al Activo relacionado.

Si no hay ninguna coincidencia, el sistema muestra **"Sin resultados para "{término}"."**

## Exportaciones (Excel/PDF)

El sistema ofrece exportación a archivo en dos puntos distintos, ambos de procesamiento
**síncrono** (la descarga se genera al momento de la solicitud, sin pasar por una cola en segundo
plano como sí ocurre con las importaciones):

1. **Exportación del listado de Activos**, disponible desde la pantalla de Activos. Usa el mismo
   conjunto de filtros que el listado en pantalla (categoría, estado, empresa, etc.), requiere el
   permiso **`Exports.Create`** y tiene un tope de **10,000 filas** por exportación. Genera un
   archivo con las columnas: Folio, Categoría, Marca, Modelo, Serie, Estado, Condición, Ubicación.
2. **Exportación de los paneles de Reportes** (Garantías por vencer, Existencias bajas, KPIs de
   mantenimiento — ver más abajo), disponible desde cada tarjeta de la pantalla `/reports` que
   incluya el botón correspondiente. Requiere el permiso **`Reports.Export`** y reutiliza
   exactamente la misma consulta que ya está mostrando el panel en pantalla, respetando los
   filtros que la persona usuaria tenga aplicados en ese momento.

**Formatos disponibles**: Excel (.xlsx) y PDF, en ambos casos. Los archivos PDF usan una fuente
embebida (Liberation Sans) para garantizar que se vean igual sin importar el sistema operativo del
servidor. El nombre del archivo descargado incluye una marca de fecha/hora en UTC.

> **Nota importante**: el permiso `Reports.Export` es **independiente** de los permisos para
> **ver** los reportes (`Reports.Read` / `Reports.ReadConsolidated`). Esto significa que, en
> teoría, una persona podría tener permiso para exportar un reporte que no puede ver en pantalla,
> o viceversa — son combinaciones de permisos que un administrador debe asignar con cuidado.

Si se solicita un formato no soportado, el sistema responde **"Formato de exportación no
soportado."**

## Reportes

La pantalla `/reports` (ver captura ![Captura de pantalla](screenshots/045_reportes.png)) concentra los paneles
operativos de solo lectura del sistema. En la parte superior tiene un botón para alternar entre
**"Ver consolidado (todas las empresas)"** y **"Ver por empresa"**, y muestra 6 tarjetas de
indicador (StatTiles) con cifras generales antes de los cuatro paneles detallados.

El acceso a la pantalla depende de dos permisos distintos según la vista elegida:

- Vista consolidada: **`Reports.ReadConsolidated`**. Sin este permiso: **"No tienes permiso para
  consultar reportes consolidados (Reports.ReadConsolidated)."**
- Vista por empresa: **`Reports.Read`**. Sin este permiso: **"No tienes permiso para consultar
  reportes consolidados (Reports.Read)."**

A continuación, el detalle de cada uno de los cuatro paneles:

### Panel 1 — Resumen de inventario

| | |
|---|---|
| **Objetivo** | Dar un panorama general de cuántos activos hay y cómo están distribuidos. |
| **Campos que muestra** | Gráfico de barras con el conteo de activos agrupado por estado (según la máquina de estados de Activos) y por categoría. |
| **Filtros** | Únicamente el alternador consolidado/por empresa de la parte superior de la pantalla; no tiene filtros propios adicionales. |
| **Cómo exportarlo** | **No es exportable.** Es el único de los cuatro paneles sin botón de exportación — no lo confunda con los otros tres, que sí lo tienen. |

### Panel 2 — KPIs de mantenimiento

| | |
|---|---|
| **Objetivo** | Medir el desempeño del proceso de mantenimiento por categoría de activo. |
| **Campos que muestra** | MTTR (tiempo medio de reparación) y MTBF (tiempo medio entre fallas), desglosados por categoría de activo. |
| **Filtros** | Alternador consolidado/por empresa. |
| **Cómo exportarlo** | Botón de exportación en la propia tarjeta ("exportar Excel/PDF"), sujeto al permiso `Reports.Export`; genera el archivo con los mismos datos que se ven en pantalla en ese momento. |

### Panel 3 — Garantías por vencer

| | |
|---|---|
| **Objetivo** | Anticipar qué garantías de activos están por vencer, para gestionar su renovación o reemplazo a tiempo. |
| **Campos que muestra** | Activos con garantía próxima a vencer, con su proveedor y fecha de vencimiento. |
| **Filtros** | Un filtro numérico de **días hacia adelante** (por ejemplo, mostrar lo que vence en los próximos 30, 60 o 90 días), además del alternador consolidado/por empresa. |
| **Cómo exportarlo** | Botón de exportación en la tarjeta, sujeto a `Reports.Export`; respeta el filtro de días que tenga seleccionado en ese momento. |

### Panel 4 — Existencias bajas

| | |
|---|---|
| **Objetivo** | Identificar consumibles cuya existencia actual cayó por debajo del mínimo definido, para reabastecer a tiempo. |
| **Campos que muestra** | Consumibles por debajo de su mínimo, con su nombre, SKU y existencia actual frente al mínimo configurado. |
| **Filtros** | Alternador consolidado/por empresa (no tiene filtros adicionales propios). |
| **Cómo exportarlo** | Botón de exportación en la tarjeta, sujeto a `Reports.Export`. |
