## 6.4 Consumibles, Repuestos y Garantías

### Introducción: tres conceptos que no deben confundirse

Este módulo agrupa tres tipos de información que, aunque conviven en el mismo grupo del menú
("Inventario y movimientos" para Consumibles y Refacciones, y "Activos" para Garantías), responden
a lógicas de negocio completamente distintas. Antes de usar cualquiera de los procesos de este
capítulo es importante entender la diferencia:

- **Consumible**: es un insumo que se controla **por cantidad**, no de forma individual. Ejemplos
  típicos son tóner, cables, pilas o etiquetas. El sistema nunca sabe "cuál" cable en particular se
  usó, solo cuántas unidades hay en existencia (`Existencia`) y, opcionalmente, cuál es el mínimo
  aceptable antes de generar una alerta. Cada consumible no tiene número de serie ni se vincula
  directamente a un activo específico; su movimiento se registra contra un almacén.
- **Repuesto (o refacción)**: es una pieza física **identificada individualmente** mediante un
  número de serie único. A diferencia del consumible, el sistema sí sabe exactamente cuál pieza es
  cuál, en dónde está (en un almacén o instalada en un activo concreto) y conserva el historial
  completo de cada instalación y retiro. Un repuesto tiene un ciclo de vida propio: existencia →
  instalado → existencia → ... → baja.
- **Garantía**: es un registro de cobertura (de fábrica, extendida o de terceros) vinculado a un
  activo específico, con vigencia, proveedor y términos.

> **Aclaración importante — dos lugares distintos donde puede haber información de garantía**
>
> El sistema tiene **dos mecanismos separados** para registrar información de garantía de un
> activo, y no están sincronizados entre sí. Es fundamental que usted los distinga para no
> confundirse al buscar o interpretar datos de vigencia:
>
> 1. **Los campos de contrato del propio Activo** (fecha de inicio y fin de garantía, y contrato de
>    soporte), que se editan desde la ficha del activo, en su información contractual.
> 2. **El módulo independiente de Garantías** descrito en este capítulo (pantalla `/warranties`),
>    que permite registrar **varias** coberturas distintas para un mismo activo a lo largo de su
>    vida útil (por ejemplo, la garantía original de fábrica y, más adelante, una extensión
>    contratada con un proveedor distinto).
>
> El sistema fue diseñado deliberadamente así porque un mismo activo puede acumular más de una
> cobertura de garantía a lo largo del tiempo, y los campos simples del activo solo alcanzan para
> guardar una vigencia a la vez. **Sin embargo, el sistema no concilia ni sincroniza automáticamente
> ambos lugares**: es perfectamente posible que la ficha del activo muestre una fecha de vigencia de
> garantía y que el módulo de Garantías muestre una fecha distinta (o varias garantías) para ese
> mismo activo, sin que ninguna de las dos se actualice cuando se cambia la otra. Si necesita saber
> la vigencia "oficial" de garantía de un activo, revise **ambos lugares** y, ante una discrepancia,
> confirme con quien administra los contratos de la empresa cuál de los dos registros es el vigente.
> Este capítulo documenta únicamente el módulo independiente de Garantías; los campos de contrato
> del activo se documentan en el capítulo dedicado a Activos.

Con esta distinción clara, a continuación se documentan los ocho procesos disponibles: alta y
movimiento de consumibles, alta e instalación/retiro/baja de refacciones, y alta/edición de
garantías.

---

### Registrar un consumible

#### Objetivo

Dar de alta un nuevo tipo de consumible en el catálogo de la empresa activa, definiendo su nombre,
unidad de medida y, opcionalmente, un umbral mínimo de existencia para efectos de alerta.

#### Cuándo utilizarlo

Cuando la organización empieza a controlar un insumo que antes no estaba registrado en el sistema
(por ejemplo, un nuevo modelo de tóner o un tipo de cable que se compra con regularidad), antes de
poder registrar cualquier entrada o salida de existencia de ese insumo.

#### Paso a paso detallado

1. En el menú, ingrese a **Inventario y movimientos → Consumibles**. Se muestra el listado de
   consumibles de la empresa activa (ver captura ![Captura de pantalla](screenshots/030_consumibles_lista.png)).
2. Presione el botón **"Nuevo consumible"**.
3. Se abre el formulario de alta (ver captura ![Captura de pantalla](screenshots/031_consumibles_nuevo.png)).
4. Capture el **Nombre** del consumible (obligatorio).
5. Capture el **SKU**, si su organización maneja uno (opcional).
6. Capture la **Unidad de medida** (obligatorio) — por ejemplo, "pieza", "caja", "metro".
7. Capture, si lo desea, la **Existencia mínima** para que el sistema pueda marcarlo como "bajo
   mínimo" cuando la cantidad disponible caiga por debajo de ese número.
8. Presione **"Registrar consumible"**.
9. El sistema redirige automáticamente a la ficha de detalle del consumible recién creado, con
   existencia inicial en cero.

#### Campos

| Campo | Obligatorio | Longitud / restricción |
|---|---|---|
| Nombre | Sí | Máximo 200 caracteres |
| SKU | No | Máximo 100 caracteres |
| Unidad de medida | Sí | Máximo 50 caracteres |
| Existencia mínima | No | Numérico, no puede ser negativo |

#### Botones y acciones

- **"Nuevo consumible"** (en el listado): abre el formulario de alta.
- **"Registrar consumible"** (en el formulario): envía el alta y, si es exitosa, redirige al
  detalle del consumible.

#### Reglas de negocio

- El consumible se crea siempre asociado a la empresa activa; no se puede reasignar a otra empresa
  después (no existe comando de transferencia de empresa para consumibles).
- El consumible se crea con existencia en cero. Para tener existencia disponible, debe registrarse
  al menos un movimiento de entrada (ver proceso 6.2).
- La existencia mínima es puramente informativa: sirve para el badge "Bajo mínimo" en el listado y
  para el reporte de existencias bajas; no bloquea ninguna operación.

#### Mensajes del sistema

- Validación del formulario si falta algún campo obligatorio: **"Nombre y unidad de medida son
  obligatorios."**
- Errores de dominio, si el backend los detecta: **"El nombre del consumible es obligatorio."**,
  **"La unidad de medida del consumible es obligatoria."**, **"La existencia mínima no puede ser
  negativa."**
- Si no tiene el permiso correspondiente para consultar el listado: **"No tienes permiso para
  consultar consumibles (Consumables.Read)."**

#### Casos especiales

- **No es posible editar un consumible ya registrado desde esta versión del sistema.** Aunque
  técnicamente existe la operación en el servidor, ningún formulario de la interfaz la utiliza. Si
  se equivocó al capturar el nombre, el SKU o la unidad de medida de un consumible, no hay manera
  de corregirlo desde la pantalla: solo puede seguir registrando movimientos de existencia sobre el
  registro tal como quedó dado de alta. Revise cuidadosamente los datos antes de presionar
  "Registrar consumible".

#### Resultado esperado

El consumible queda visible en el listado de `/consumables` con existencia en cero, listo para
recibir su primer movimiento de entrada.

#### Buenas prácticas

- Defina un SKU consistente con el que ya use su organización en otros sistemas (compras,
  almacén), para facilitar la conciliación.
- Capture la existencia mínima desde el alta si ya conoce el umbral deseado; agregarla después
  requeriría edición, que no está disponible.
- Revise el nombre y la unidad de medida con cuidado antes de guardar, ya que no podrá corregirlos
  posteriormente.

---

### Registrar un movimiento de existencia de un consumible (entrada/salida)

#### Objetivo

Aumentar o disminuir la existencia disponible de un consumible ya registrado, dejando un registro
inmutable de cada movimiento.

#### Cuándo utilizarlo

Cada vez que ingrese mercancía al almacén (compra, existencia inicial) o que se consuma o ajuste la
cantidad disponible de un consumible (consumo, ajuste por conteo físico, etc.).

#### Paso a paso detallado

1. Desde el listado de consumibles (![Captura de pantalla](screenshots/030_consumibles_lista.png)), presione el nombre
   del consumible sobre el que desea registrar el movimiento.
2. Se abre la ficha de detalle del consumible, con su existencia actual y el badge de "Bajo
   mínimo" cuando corresponda. [Captura pendiente: detalle de consumible.]
3. En el formulario "Registrar movimiento", seleccione la **Dirección**: Entrada o Salida.
4. Seleccione el **Motivo**: Existencia inicial, Compra, Consumo o Ajuste.
5. Seleccione el **Almacén** — únicamente se listan unidades organizacionales de tipo "Almacén"
   de la empresa activa.
6. Capture la **Cantidad** del movimiento (debe ser mayor a cero).
7. Capture **Notas** adicionales si lo desea (opcional).
8. Presione el botón de registrar el movimiento.
9. El movimiento aparece de inmediato en el historial de movimientos de la ficha, y la existencia
   mostrada en el encabezado se actualiza.

#### Campos

| Campo | Obligatorio | Restricción |
|---|---|---|
| Dirección | Sí | Entrada / Salida |
| Motivo | Sí | Existencia inicial / Compra / Consumo / Ajuste |
| Almacén | Sí | Debe ser una unidad organizacional de tipo "Almacén" de la misma empresa |
| Cantidad | Sí | Numérico, mayor a cero |
| Notas | No | Máximo 1000 caracteres |

#### Botones y acciones

- Botón de registrar movimiento en la ficha de detalle del consumible: envía el movimiento y
  refresca la existencia y el historial.

#### Reglas de negocio

- Todo cambio de existencia genera un registro de movimiento (folio, dirección, motivo, cantidad,
  fecha) que **no puede editarse ni borrarse** después de creado.
- La existencia nunca puede quedar negativa: si una salida deja la existencia por debajo de cero,
  el sistema rechaza la operación.
- El almacén seleccionado se vuelve a validar en el servidor en cada movimiento (existencia real,
  tipo "Almacén", misma empresa que el consumible) — el sistema nunca confía únicamente en lo que
  ofrece la lista desplegable del formulario.
- Este proceso queda registrado en la bitácora de auditoría del sistema.
- Si no existe ninguna unidad organizacional de tipo "Almacén" configurada en la empresa, no será
  posible registrar el movimiento hasta que se cree una desde el módulo de Estructura organizacional.

#### Mensajes del sistema

- Validación de formulario si falta algún campo: **"Almacén, dirección, motivo y una cantidad
  mayor a cero son obligatorios."**
- Si no hay almacenes disponibles en la empresa: **"No hay unidades de tipo Almacén en la
  estructura organizacional."**
- Error de dominio si la cantidad no es mayor a cero: **"La cantidad del movimiento de existencia
  debe ser mayor a cero."**
- Error de dominio si el movimiento dejaría la existencia negativa: **"Este movimiento dejaría la
  existencia del consumible en negativo."**
- Error de conflicto si el almacén no es válido para la empresa: **"El almacén indicado no es
  válido para esta empresa."**
- Historial vacío (consumible recién creado, sin movimientos todavía): **"Sin movimientos
  todavía."**

#### Casos especiales

- **Los movimientos son verdaderamente inmutables.** El sistema no ofrece un comando dedicado de
  "movimiento compensatorio": si registró un movimiento por error, la única corrección posible es
  capturar un **nuevo** movimiento en dirección contraria, normalmente con motivo "Ajuste", que
  compense la cantidad equivocada. El movimiento original permanecerá siempre visible en el
  historial, tal como quedó capturado.
- El indicador de "Bajo mínimo" en el listado general y el reporte de existencias bajas usan
  criterios de comparación ligeramente distintos entre sí (uno más estricto que el otro), por lo
  que en casos límite podría ver el badge activado en un lugar y no en el otro para el mismo
  consumible. Ante cualquier duda sobre si un consumible está realmente por debajo de su mínimo,
  confíe en el valor numérico de existencia y mínimo mostrados, más que en el badge.

#### Resultado esperado

La existencia del consumible se actualiza de inmediato y el movimiento queda agregado al final del
historial de movimientos de la ficha, con su folio, dirección, motivo, cantidad y fecha.

#### Buenas prácticas

- Use el motivo "Existencia inicial" únicamente para la primera carga de inventario de un
  consumible recién creado; para reposiciones posteriores use "Compra".
- Capture notas descriptivas en movimientos de "Ajuste" (por ejemplo, referencia al conteo físico
  que originó la corrección), ya que no hay otro campo donde documentar el motivo real del ajuste.
- Verifique la cantidad capturada antes de confirmar: recuerde que no podrá editar ni borrar el
  movimiento después, solo compensarlo con uno nuevo.

---

### Registrar una refacción

#### Objetivo

Dar de alta una nueva refacción (repuesto) en el catálogo de la empresa activa, identificada por su
número de serie único, y ubicarla inicialmente en un almacén.

#### Cuándo utilizarlo

Cuando ingresa físicamente al almacén una pieza serializada (por ejemplo, un disco duro, una
memoria RAM o una fuente de poder) que en algún momento podrá instalarse en un activo.

#### Paso a paso detallado

1. En el menú, ingrese a **Inventario y movimientos → Refacciones**. Se muestra el listado de
   refacciones de la empresa activa (ver captura ![Captura de pantalla](screenshots/032_refacciones_lista.png)).
2. Presione el botón de alta de refacción.
3. Se abre el formulario de alta (ver captura ![Captura de pantalla](screenshots/033_refacciones_nuevo.png)).
4. Capture el **Nombre** de la refacción (obligatorio).
5. Capture el **Número de parte**, si aplica (opcional).
6. Capture el **Número de serie** (obligatorio) — debe ser único dentro de la empresa.
7. Seleccione el **Almacén** donde queda ubicada la refacción (obligatorio; solo se listan
   unidades de tipo "Almacén"). Si no existe ninguna, el sistema muestra un enlace directo al
   módulo de Estructura organizacional para crear una.
8. Presione el botón de registrar.
9. El sistema redirige a la ficha de detalle de la refacción recién creada, en estado "En
   existencia".

#### Campos

| Campo | Obligatorio | Restricción |
|---|---|---|
| Nombre | Sí | Máximo 200 caracteres |
| Número de parte | No | Máximo 100 caracteres |
| Número de serie | Sí | Máximo 100 caracteres; único por empresa (no globalmente) |
| Almacén | Sí | Unidad organizacional de tipo "Almacén" |

#### Botones y acciones

- Botón de alta en el listado: abre el formulario de nueva refacción.
- Botón de registrar en el formulario: envía el alta y redirige a la ficha de detalle.

#### Reglas de negocio

- El número de serie debe ser único **dentro de la empresa**, no de forma global en todo el
  sistema: dos empresas distintas podrían, en teoría, tener refacciones con el mismo número de
  serie registradas de forma independiente.
- La refacción se crea siempre en estado "En existencia" (`InStock`), ubicada en el almacén
  indicado.
- No existe comando para reasignar la refacción a otra empresa una vez creada.

#### Mensajes del sistema

- Validación de formulario si falta algún campo: **"Nombre, número de serie y almacén son
  obligatorios."**
- Error de dominio si falta el nombre: **"El nombre de la refacción es obligatorio."**
- Error de dominio si falta el número de serie: **"El número de serie de la refacción es
  obligatorio."**
- Error de conflicto si ya existe una refacción con ese número de serie en la empresa: **"Ya
  existe una refacción con ese número de serie en esta empresa."**
- Error de conflicto si el almacén no es válido para la empresa: **"El almacén indicado no es
  válido para esta empresa."**
- Listado vacío: **"No hay refacciones registradas todavía."**
- Sin permiso de consulta: **"No tienes permiso para consultar refacciones (SpareParts.Read)."**

#### Casos especiales

- Si intenta registrar dos refacciones con el mismo número de serie dentro de la misma empresa, el
  sistema rechazará la segunda con el mensaje de conflicto citado arriba. Verifique el número de
  serie físico de la pieza antes de capturarlo para evitar este rechazo.

#### Resultado esperado

La refacción queda visible en el listado de `/spare-parts`, en estado "En existencia", ubicada en
el almacén seleccionado, y disponible para ser instalada en un activo (ver proceso 6.4).

#### Buenas prácticas

- Capture el número de parte del fabricante siempre que esté disponible en la etiqueta física de
  la pieza; facilita identificarla en listados largos junto al nombre.
- Verifique con cuidado el número de serie al capturarlo — es el identificador que después
  aparecerá en todo el historial de instalación/retiro de la pieza.

---

### Instalar una refacción en un activo

#### Objetivo

Registrar que una refacción que está en existencia pasa a estar instalada físicamente en un activo
específico.

#### Cuándo utilizarlo

Cuando técnicamente se instala una pieza serializada (disco, memoria, fuente, etc.) dentro de un
equipo dado de alta como activo en el sistema.

#### Paso a paso detallado

1. Desde el listado de refacciones (![Captura de pantalla](screenshots/032_refacciones_lista.png)), abra la refacción
   que desea instalar (debe estar en estado "En existencia"). [Captura pendiente: detalle de
   refacción en existencia con formulario de instalación.]
2. En la ficha de detalle, localice el formulario "Instalar".
3. Seleccione el **Activo** destino en el desplegable. Este desplegable solo muestra activos que
   se encuentran en estado "En almacén".
4. Presione el botón **"Instalar"**.
5. La refacción cambia a estado "Instalada" y queda vinculada al activo seleccionado; el
   movimiento se agrega al historial de instalación/retiro de la ficha.

#### Campos

| Campo | Obligatorio | Restricción |
|---|---|---|
| Activo | Sí | Se listan únicamente activos en estado "En almacén" |

#### Botones y acciones

- **"Instalar"**: ejecuta la instalación de la refacción en el activo seleccionado.

#### Reglas de negocio

- Solo una refacción que se encuentra en estado "En existencia" puede instalarse.
- La refacción y el activo deben pertenecer a la misma empresa.
- Esta operación queda registrada en la bitácora de auditoría.

#### Mensajes del sistema

- Error de dominio si la refacción no está en existencia: **"Solo una refacción en existencia
  puede instalarse."**
- Error de conflicto si la refacción y el activo no son de la misma empresa: **"La refacción y el
  activo deben pertenecer a la misma empresa."**

#### Casos especiales

- **Importante — el sistema no valida en el servidor que el activo destino esté realmente
  disponible en almacén al momento de instalar la refacción**, a pesar de que la pantalla, por
  diseño, solo ofrece en el desplegable a los activos que en ese momento aparecen como "En
  almacén". Esto significa que, en teoría, si el estado del activo cambiara entre que se cargó la
  pantalla y que se confirma la instalación, la operación podría completarse contra un activo que
  ya no está realmente disponible, sin que el sistema lo detecte ni lo impida. Tenga esto en cuenta
  en flujos de trabajo con varias personas operando simultáneamente sobre los mismos activos, y
  confirme visualmente el estado del activo en su propia ficha si tiene dudas.

#### Resultado esperado

La refacción pasa a estado "Instalada", queda asociada al activo indicado, y en el historial de la
ficha aparece un nuevo renglón con el folio del activo y la fecha de instalación, mostrando
"actualmente instalada" como estado del periodo.

#### Buenas prácticas

- Instale la refacción en el sistema en el mismo momento en que se realiza físicamente la
  instalación, para mantener el historial alineado con la realidad.
- Antes de instalar, confirme en la ficha del activo que efectivamente se encuentra disponible,
  ya que el sistema no hace esta verificación por usted al momento de guardar.

---

### Retirar una refacción de un activo

#### Objetivo

Registrar que una refacción actualmente instalada en un activo se retira físicamente y regresa a
existencia en un almacén.

#### Cuándo utilizarlo

Cuando se desinstala físicamente una pieza de un equipo (por reemplazo, reparación, reutilización
en otro equipo, etc.) y debe volver al inventario disponible.

#### Paso a paso detallado

1. Desde el listado de refacciones, abra la refacción que se encuentra en estado "Instalada".
   [Captura pendiente: detalle de refacción instalada con formulario de retiro.]
2. En la ficha de detalle, localice el formulario "Retirar".
3. Seleccione el **Almacén destino** al que regresará la pieza.
4. Presione el botón **"Retirar"**.
5. La refacción cambia a estado "En existencia", ubicada en el almacén seleccionado; el registro
   de instalación vigente se cierra con la fecha de retiro.

#### Campos

| Campo | Obligatorio | Restricción |
|---|---|---|
| Almacén destino | Sí | Unidad organizacional de tipo "Almacén" |

#### Botones y acciones

- **"Retirar"**: ejecuta el retiro de la refacción y la regresa a existencia.

#### Reglas de negocio

- Solo una refacción en estado "Instalada" puede retirarse.
- Debe existir un registro de instalación vigente asociado a la refacción; si por alguna
  inconsistencia no lo hay, la operación se rechaza.
- El almacén destino se valida igual que en los demás procesos (tipo "Almacén", misma empresa).

#### Mensajes del sistema

- Error de dominio si la refacción no está instalada: **"Solo una refacción instalada puede
  retirarse."**
- Error de dominio si no se encuentra el registro de instalación vigente: **"No se encontró el
  registro de instalación vigente de esta refacción."**
- Error de conflicto si el almacén no es válido para la empresa: **"El almacén indicado no es
  válido para esta empresa."**

#### Casos especiales

- Si el activo del que se retira la refacción fue eliminado o transferido de forma que ya no es
  visible para la empresa actual, el historial de instalación de la refacción **igual conserva y
  muestra ese registro** (el sistema deliberadamente no lo oculta), mostrando la leyenda
  **"(activo eliminado)"** en lugar del folio, para no perder trazabilidad de la pieza.

#### Resultado esperado

La refacción vuelve a estado "En existencia" en el almacén indicado, y el renglón correspondiente
en el historial de instalación/retiro se actualiza con la fecha de retiro en lugar de "actualmente
instalada".

#### Buenas prácticas

- Retire la refacción en el sistema en cuanto se retire físicamente, para que el historial de
  instalación refleje con precisión cuánto tiempo estuvo instalada.
- Verifique el almacén destino antes de confirmar, especialmente si su organización distingue
  entre varios almacenes físicos.

---

### Dar de baja una refacción

#### Objetivo

Marcar una refacción como definitivamente fuera de uso (baja), cuando ya no será reinstalada ni
reutilizada.

#### Cuándo utilizarlo

Cuando una pieza serializada se descompone de forma irreparable, se pierde, o se decide desecharla
y ya no debe seguir apareciendo como disponible en el inventario de refacciones.

#### Paso a paso detallado

1. Desde el listado de refacciones, abra la refacción que se encuentra en estado "En existencia".
   [Captura pendiente: detalle de refacción con botón de baja.]
2. Localice el botón **"Dar de baja"**.
3. Presione el botón.
4. La refacción cambia de inmediato a estado "Dada de baja", sin ningún cuadro de confirmación
   intermedio.

#### Campos

Este proceso no tiene formulario ni campos: es una acción de un solo botón sobre la refacción ya
existente.

#### Botones y acciones

- **"Dar de baja"**: cambia el estado de la refacción a "Dada de baja" de forma permanente.

#### Reglas de negocio

- Solo una refacción en estado "En existencia" puede darse de baja. Si está instalada, primero
  debe retirarse (proceso 6.5).
- Una vez dada de baja, la refacción no ofrece ninguna acción adicional en su ficha: la pantalla
  únicamente informa que ya fue dada de baja.
- No existe ningún comando para revertir la baja de una refacción ni para eliminarla del sistema.

#### Mensajes del sistema

- Error de dominio si la refacción no está en existencia: **"Solo una refacción en existencia
  puede darse de baja (retírala primero si está instalada)."**
- Mensaje mostrado en la ficha de una refacción ya dada de baja: **"Esta refacción ya fue dada de
  baja."**

#### Casos especiales

- **Advertencia — esta acción es irreversible y el botón "Dar de baja" no solicita ninguna
  confirmación antes de ejecutarse.** A diferencia de otras operaciones destructivas del sistema
  que muestran un cuadro de diálogo de confirmación, aquí basta con un solo clic para completar la
  baja de forma permanente, sin posibilidad de deshacerla desde la interfaz. Tenga especial cuidado
  de estar viendo la refacción correcta antes de presionar este botón — verifique el nombre y,
  sobre todo, el número de serie mostrados en la ficha.

#### Resultado esperado

La refacción queda en estado "Dada de baja" de forma permanente, deja de estar disponible para
instalación, y su ficha muestra únicamente el mensaje de que ya fue dada de baja, sin más acciones
disponibles.

#### Buenas prácticas

- Antes de presionar "Dar de baja", confirme visualmente el número de serie de la refacción en
  pantalla, ya que no habrá una segunda oportunidad de cancelar la operación.
- Si existe alguna duda sobre si la pieza realmente debe darse de baja o solo requiere
  mantenimiento, no ejecute esta acción hasta confirmarlo — recuerde que es irreversible.
- Dé de baja únicamente refacciones que estén en existencia; si está instalada, retírela primero
  siguiendo el proceso 6.5.

---

### Registrar una garantía

#### Objetivo

Crear un nuevo registro de cobertura de garantía vinculado a un activo específico, dentro del
módulo independiente de Garantías (recuerde la aclaración de la sección 6.0 sobre la coexistencia
con los campos de garantía propios del activo).

#### Cuándo utilizarlo

Cuando se adquiere o se identifica una cobertura de garantía para un activo — de fábrica, extendida
o de un tercero — y se desea llevar un registro formal de su vigencia, proveedor y términos dentro
del sistema.

#### Paso a paso detallado

1. En el menú, ingrese a **Activos → Garantías**. Se muestra el listado de garantías registradas
   (ver captura ![Captura de pantalla](screenshots/034_garantias_lista.png)).
2. Presione el botón de alta de garantía.
3. Se abre el formulario de registro (ver captura ![Captura de pantalla](screenshots/035_garantias_nuevo.png)).
4. Seleccione el **Activo** al que corresponde la garantía (obligatorio).
5. Seleccione el **Tipo**: Fabricante, Extendida o Terceros (obligatorio).
6. Capture el **Proveedor** (obligatorio).
7. Capture la **Fecha de inicio** y la **Fecha de fin** de la vigencia (ambas obligatorias).
8. Capture, si lo desea, los **Términos** de la cobertura en texto libre (opcional).
9. Presione el botón de registrar.
10. El sistema redirige al **listado** de garantías (no a una ficha de detalle individual, ya que
    esta no existe para garantías).

#### Campos

| Campo | Obligatorio | Restricción |
|---|---|---|
| Activo | Sí | Selección de un activo existente de la empresa activa |
| Tipo | Sí | Fabricante / Extendida / Terceros |
| Proveedor | Sí | Máximo 200 caracteres |
| Fecha de inicio | Sí | Fecha |
| Fecha de fin | Sí | Fecha; debe ser posterior o igual a la fecha de inicio |
| Términos | No | Texto libre, máximo 2000 caracteres |

#### Botones y acciones

- Botón de alta en el listado: abre el formulario de nueva garantía.
- Botón de registrar en el formulario: envía el alta y redirige al listado de garantías.

#### Reglas de negocio

- La fecha de inicio debe ser anterior o igual a la fecha de fin; el sistema rechaza combinaciones
  inválidas.
- Un mismo activo puede tener múltiples garantías registradas a lo largo del tiempo (por ejemplo,
  la de fábrica y, después, una extensión contratada); el sistema no limita la cantidad de
  garantías por activo.
- Los términos son texto libre; **no es posible adjuntar un documento** a la garantía desde esta
  pantalla, aun cuando el sistema cuenta con un módulo de documentos independiente — ambos módulos
  no están integrados entre sí en esta versión.
- No existe endpoint para reasignar la garantía a otro activo ni a otra empresa una vez creada.

#### Mensajes del sistema

- Validación de formulario si falta algún campo obligatorio: **"Activo, tipo, proveedor y fechas
  son obligatorios."**
- Error de dominio si falta el proveedor: **"El proveedor de la garantía es obligatorio."**
- Error de dominio si las fechas son inconsistentes: **"La fecha de inicio de la garantía debe ser
  anterior o igual a la fecha de fin."**
- Listado vacío: **"No hay garantías registradas todavía."**
- Sin permiso de consulta: **"No tienes permiso para consultar garantías (Warranties.Read)."**

#### Casos especiales

- A diferencia de Consumibles y Refacciones, tras registrar una garantía **el sistema redirige al
  listado, no a una ficha de detalle** — porque no existe una pantalla de detalle individual para
  garantías. Para volver a ver o modificar la garantía recién creada, localícela en el listado
  `/warranties`.
- El listado resalta en color rojo las garantías cuya vigencia ya venció, para facilitar
  identificarlas de un vistazo.
- Recuerde la aclaración de la sección 6.0: registrar aquí una garantía **no actualiza** los campos
  de garantía propios de la ficha del activo, y viceversa. Son dos registros independientes.

#### Resultado esperado

La garantía queda visible en el listado `/warranties`, asociada al activo seleccionado, mostrando
su tipo, proveedor y vigencia, con la acción "Editar" disponible en el mismo renglón.

#### Buenas prácticas

- Al registrar una nueva garantía, revise el listado para confirmar si el activo ya tenía
  coberturas previas registradas, y evalúe si conviene mantenerlas o si alguna quedó obsoleta.
- Documente en el campo de Términos cualquier condición relevante de la cobertura (alcance,
  exclusiones, datos de contacto del proveedor), ya que no hay forma de adjuntar un documento de
  respaldo desde esta pantalla.
- Si la garantía relevante para su organización es la misma que ya se refleja en los campos de
  contrato del activo, considere mantener ambos registros alineados manualmente, ya que el sistema
  no lo hace por usted.

---

### Editar una garantía

#### Objetivo

Corregir o actualizar los datos de una garantía ya registrada (tipo, proveedor, vigencia o
términos), sin poder reasignarla a un activo distinto.

#### Cuándo utilizarlo

Cuando cambian las condiciones de una cobertura ya registrada (por ejemplo, se renegocia la
vigencia con el proveedor) o se detecta un error de captura en el registro original.

#### Paso a paso detallado

1. En el listado de garantías (![Captura de pantalla](screenshots/034_garantias_lista.png)), localice el renglón de la
   garantía que desea modificar y presione **"Editar"**.
2. Se abre la pantalla de edición, que reutiliza el mismo formulario de alta, con los datos
   actuales precargados. [Captura pendiente: pantalla de edición de garantía.]
3. Los mismos campos del alta están disponibles para modificarse, **salvo el Activo**, que no
   puede reasignarse desde esta pantalla.
4. Modifique el **Tipo**, **Proveedor**, **Fecha de inicio**, **Fecha de fin** o **Términos** según
   corresponda.
5. Presione el botón **"Guardar cambios"**.
6. El sistema aplica los cambios y regresa al listado de garantías con los datos actualizados.

#### Campos

| Campo | Obligatorio | Restricción |
|---|---|---|
| Activo | — | No editable; se muestra de solo lectura |
| Tipo | Sí | Fabricante / Extendida / Terceros |
| Proveedor | Sí | Máximo 200 caracteres |
| Fecha de inicio | Sí | Fecha |
| Fecha de fin | Sí | Fecha; debe ser posterior o igual a la fecha de inicio |
| Términos | No | Texto libre, máximo 2000 caracteres |

#### Botones y acciones

- **"Guardar cambios"**: aplica las modificaciones a la garantía existente.

#### Reglas de negocio

- El activo asociado a la garantía **no puede cambiarse** desde la edición; si la garantía se
  registró contra el activo equivocado, no hay forma de corregirlo en esta pantalla (deberá
  evaluarse con un administrador si conviene registrar una garantía nueva en el activo correcto).
- Se mantiene la misma validación cruzada de fechas que en el alta.
- No existe pantalla de "solo lectura" separada para consultar una garantía: la única forma de ver
  el detalle completo de una garantía es entrando a su pantalla de edición.

#### Mensajes del sistema

- Validación de formulario si falta algún campo obligatorio: **"Tipo, proveedor y fechas son
  obligatorios."**
- Error de dominio si falta el proveedor: **"El proveedor de la garantía es obligatorio."**
- Error de dominio si las fechas son inconsistentes: **"La fecha de inicio de la garantía debe ser
  anterior o igual a la fecha de fin."**

#### Casos especiales

- **No existe un endpoint para consultar una garantía de forma individual en el servidor.** La
  pantalla de edición obtiene sus datos filtrando, del lado del navegador, sobre el listado
  completo de garantías de la empresa. En la práctica esto no cambia la forma de uso para usted,
  pero explica por qué, si tiene una cantidad muy grande de garantías registradas, la pantalla
  podría tardar un poco más en cargar mientras filtra el resultado.
- Si necesita corregir el activo al que pertenece una garantía, recuerde que esta pantalla no lo
  permite; consulte con un administrador la mejor forma de proceder (por ejemplo, registrar una
  garantía nueva en el activo correcto y dejar constancia de que la anterior quedó mal capturada).

#### Resultado esperado

La garantía se actualiza con los nuevos datos y estos se reflejan de inmediato en el listado de
`/warranties`, incluyendo el color rojo si la nueva fecha de fin ya está vencida.

#### Buenas prácticas

- Antes de guardar, verifique especialmente las fechas de vigencia, ya que un error aquí puede
  hacer que el sistema marque incorrectamente una garantía como vencida o vigente en el listado.
- Si la corrección que necesita hacer involucra cambiar el activo asociado, no intente forzarlo
  desde esta pantalla — no es posible — y resuelva el caso registrando un nuevo registro de
  garantía en el activo correcto.
- Aproveche el campo de Términos para dejar una nota del motivo de la edición cuando el cambio sea
  relevante para auditoría interna de su organización, ya que esta operación no se registra en la
  bitácora de auditoría del sistema.
