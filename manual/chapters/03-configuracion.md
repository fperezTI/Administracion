# 5. Configuración inicial

> **Importante — esta sección reemplaza cualquier instrucción anterior que indique dar de alta el primer administrador, la primera empresa o los primeros roles "directamente en la base de datos" o "por script".** Esas instrucciones correspondían a una versión temprana del sistema en la que la interfaz de administración todavía no existía. **Hoy esa interfaz existe y está completamente operativa de extremo a extremo.** Todo el arranque de un sistema recién desplegado —incluida la creación del primer usuario con privilegios totales— se realiza desde la propia aplicación web, sin tocar la base de datos ni ejecutar scripts manuales.

Un sistema recién desplegado no tiene empresas, ni unidades organizativas, ni roles, ni usuarios cargados. La puesta en marcha sigue siempre esta secuencia, y no puede alterarse:

**Paso 1 — Bootstrap automático del primer superadministrador.**
La primera persona que inicia sesión con su cuenta de Microsoft Entra ID en la vida del sistema recibe automáticamente, sin ninguna acción manual, un rol llamado **"Super Administrador"** con **todos** los permisos del catálogo (a la fecha, 69 permisos). Esto ocurre una sola vez: es una excepción de seguridad deliberada, pensada exclusivamente para resolver el problema de "¿quién administra al primer administrador?", y queda registrada en la auditoría del sistema. La segunda persona que inicia sesión, y todas las siguientes, **no** reciben ningún rol ni acceso automático — deben ser dadas de alta manualmente por alguien que ya tenga los permisos correspondientes (ver §8).

**Paso 2 — Crear la primera empresa.**
Ese primer superadministrador, aunque ya tiene todos los permisos, todavía no tiene acceso a ninguna empresa (el bootstrap otorga permisos, no membresías). Debe entrar a **Administración → Empresas** y crear el primer registro (ver §6.1). Sin al menos una empresa creada, el resto del sistema no tiene dónde operar.

**Paso 3 — Otorgarse a sí mismo acceso a esa empresa.**
Con la empresa ya creada, el superadministrador va a **Administración → Usuarios**, se busca a sí mismo en el listado, entra a su propio perfil y usa la sección "Empresas con acceso" para otorgarse acceso a la empresa recién creada (ver §8). Este paso es indispensable: sin él, pantallas como Estructura organizacional, Activos, Asignaciones, etc. seguirán mostrando el mensaje de "sin acceso a ninguna empresa" aunque el usuario tenga todos los permisos del catálogo.

**Paso 4 — Recién ahora, construir la configuración operativa.**
Con al menos una empresa y acceso a ella, ya es posible:
- Definir la estructura organizacional de esa empresa (§6.2).
- Crear los roles adicionales que necesite la organización y, si corresponde, dar de alta a otros usuarios asignándoles esos roles y el acceso a empresas que les corresponda (§8).
- Definir las categorías de activos y sus campos técnicos (§6.3).
- A partir de ahí, el resto de los módulos operativos (activos, asignaciones, movimientos, mantenimiento, etc.) quedan disponibles.

**Nota:** el selector de empresa activa que aparece en pantallas como Estructura organizacional es solo un filtro de navegación (`?companyId=` en la URL); no otorga acceso por sí mismo. El acceso real a una empresa se define exclusivamente desde el perfil del usuario en **Administración → Usuarios**, tal como se describe en el Paso 3.

---

## Empresas

**Objetivo.** Empresas (`/companies`) es el catálogo de las entidades legales que conviven dentro de un mismo entorno del sistema — hasta 50 empresas comparten un único tenant de Microsoft Entra ID. Es la raíz de toda la segregación multiempresa: cada activo, asignación, movimiento, etc. queda vinculado a una de las empresas de este catálogo.

![Listado de empresas](screenshots/051_empresas_lista.png)

La pantalla de listado muestra, por cada empresa registrada: Nombre comercial, Razón social, RFC/Id. fiscal, Moneda, Zona horaria, Estado (Activa/Inactiva) y un botón para Activar/Desactivar. El subtítulo de la pantalla recuerda el límite: "Hasta 50 empresas dentro de un único tenant de Entra ID." Si todavía no hay ninguna empresa cargada, se muestra el mensaje "Todavía no hay empresas registradas." Si el usuario no tiene el permiso necesario, se muestra "No tienes permiso para consultar empresas (Companies.Read)."

**Configuración.** El alta se hace desde el botón "Nueva empresa", que abre el formulario siguiente:

| Campo | Descripción | Obligatorio | Validaciones |
|---|---|---|---|
| Nombre comercial | Nombre con el que se identifica a la empresa en el día a día. | Sí | Máximo 200 caracteres. Mensaje si falta: "El nombre comercial es obligatorio." |
| Razón social | Denominación legal de la empresa. | Sí | Máximo 200 caracteres. Mensaje si falta: "La razón social es obligatoria." |
| RFC / Identificación fiscal | Identificador fiscal de la empresa. | Sí | Máximo 50 caracteres. Mensaje si falta: "La identificación fiscal es obligatoria." |
| Moneda base (ISO 4217) | Código de moneda en el que opera la empresa. | Sí | Exactamente 3 letras mayúsculas. Valor por defecto al abrir el formulario: `MXN`. Mensaje de dominio si no cumple el formato: "La moneda base debe ser un código ISO 4217 de 3 letras." |
| Zona horaria (IANA) | Zona horaria de referencia de la empresa (usada para fechas y horarios en todo el sistema). | Sí | Lista desplegable poblada con las zonas horarias IANA que reconoce el navegador. Valor por defecto: `America/Mexico_City`. Mensaje de dominio si falta: "La zona horaria es obligatoria." |

Antes de enviar el formulario, si algún campo quedó vacío o la moneda no tiene el formato correcto, el sistema muestra en el propio formulario: "Todos los campos son obligatorios y la moneda debe tener 3 letras (ISO 4217)."

![Formulario de alta de empresa](screenshots/052_empresas_nuevo.png)

**Valores permitidos.**
- Moneda base: cualquier código de 3 letras en mayúsculas (no se valida contra una lista cerrada de monedas reales, solo el formato).
- Zona horaria: cualquier valor de la lista estándar de zonas horarias IANA que expone el navegador (por ejemplo `America/Mexico_City`, `America/Monterrey`, `America/Bogota`, etc.).
- Cantidad máxima de empresas en el catálogo: **50**. Al intentar crear la número 51, el sistema responde con el error "No se pueden registrar más de 50 empresas."

**Impacto en el sistema.**
- Toda entidad transaccional (activos, asignaciones, movimientos, solicitudes, etc.) queda vinculada a una empresa de este catálogo mediante su `CompanyId`.
- El acceso de un usuario a una empresa es independiente de sus roles: un usuario puede tener el permiso `Companies.Read` y aun así no ver ninguna empresa si nadie le otorgó acceso a ella (ver §8, "Empresas con acceso").
- Desactivar una empresa ("Desactivar" en el listado) no la elimina ni borra su información; solo cambia su estado.

**Limitación conocida:** una vez creada, **no existe forma de editar el perfil de una empresa** (nombre comercial, razón social, RFC, moneda o zona horaria) desde la interfaz. Solo se puede cambiar su estado (Activa/Inactiva). Si se necesita corregir un dato de una empresa ya creada, hoy no hay una vía soportada distinta de contactar a soporte técnico. Verifique con cuidado los datos antes de confirmar el alta.

**Ejemplo de uso.** Una organización llamada "Grupo Staff" despliega el sistema por primera vez. El superadministrador bootstrapeado entra a **Administración → Empresas**, hace clic en "Nueva empresa" y completa: Nombre comercial `Grupo Staff`, Razón social `Grupo Staff`, RFC/Id. fiscal `SPC920409PY0`, Moneda base `MXN`, Zona horaria `America/Monterrey`. Al confirmar con "Crear empresa", la empresa aparece en el listado con estado "Activa".

---

## Estructura organizacional (unidades)

**Objetivo.** Estructura organizacional (`/org-units`) permite construir, empresa por empresa, un árbol jerárquico interno: unidades de negocio, direcciones, gerencias, departamentos, áreas, equipos, sucursales, ubicaciones físicas, almacenes, centros de datos o proyectos. Esta jerarquía se usa después para ubicar activos, encuadrar asignaciones y organizar reportes.

Si el usuario que entra a esta pantalla no tiene acceso a ninguna empresa, ve el mensaje: "Todavía no tienes acceso a ninguna empresa. Pide a un administrador que te agregue desde el módulo de usuarios." — en ese caso, lo que corresponde es completar primero el Paso 3 de §6 (que un administrador le otorgue acceso desde `/users/[id]`).

Con acceso a al menos una empresa, la pantalla muestra un selector de empresa (arriba, junto al buscador) y, debajo, la tabla de unidades de esa empresa: Nombre, Tipo, Código, Estado, y una columna "Mover a" con su botón "Mover". Si la empresa todavía no tiene ninguna unidad cargada, se muestra: "Esta empresa todavía no tiene estructura organizacional." Si falta el permiso correspondiente: "No tienes permiso para consultar la estructura organizacional (Structure.Read)."

![Estructura organizacional](screenshots/053_estructura_lista.png)

**Configuración.** El formulario de alta ("Nueva unidad organizacional") pide:

| Campo | Descripción | Obligatorio | Validaciones |
|---|---|---|---|
| Nombre | Nombre de la unidad (p. ej. "Almacén Central"). | Sí | Máximo 200 caracteres. Mensaje de dominio si falta: "El nombre de la unidad organizacional es obligatorio." |
| Código | Código corto identificador (p. ej. "ALM-01"). | Sí | Máximo 50 caracteres. Mensaje de dominio si falta: "El código de la unidad organizacional es obligatorio." |
| Tipo | Categoría de la unidad dentro de la jerarquía. | Sí | Debe elegirse de la lista desplegable de tipos activos; un tipo inexistente o inactivo produce un error de "no encontrado". |
| Unidad padre | Unidad de la que depende jerárquicamente. | No | Valor por defecto "Ninguna (raíz)", es decir, sin padre. Si se indica, debe pertenecer a la misma empresa; de lo contrario produce un error de "no encontrado". |

Si el nombre, el código o el tipo quedan vacíos al enviar el formulario, el sistema muestra: "Nombre, código y tipo son obligatorios." Además, el dominio impide que una unidad sea su propio padre: "Una unidad organizacional no puede ser su propio padre."

**Valores permitidos.** Los tipos de unidad organizacional disponibles corresponden a las categorías típicas de una estructura corporativa y física: unidad de negocio, dirección, gerencia, departamento, área, equipo, sucursal, ubicación física, almacén, centro de datos y proyecto.

**Impacto en el sistema.**
- **Mover una unidad** (botón "Mover" junto al selector "Mover a") reubica una unidad dentro del árbol de su misma empresa. El sistema aplica tres controles antes de permitirlo:
  - La nueva unidad padre debe pertenecer a la misma empresa — si no, el error es: "La nueva unidad padre debe pertenecer a la misma empresa."
  - El movimiento no puede crear un ciclo (por ejemplo, mover una unidad para que quede debajo de uno de sus propios descendientes) — error: "El movimiento crearía un ciclo..."
  - Existe un límite de profundidad de validación (hasta 1000 niveles); si se excede, el error es: "La jerarquía organizacional es demasiado profunda para validarse."
- **Activar/Desactivar** cambia el estado de la unidad sin eliminarla.
- No existe edición del nombre o el código de una unidad ya creada: solo se puede dar de alta, mover, activar o desactivar.

**Ejemplo de uso.** Sobre la empresa "Grupo Staff", un administrador crea primero una unidad raíz: Nombre `Dirección de Tecnología`, Código `DT`, Tipo `Dirección`, Unidad padre `Ninguna (raíz)`. Luego crea una segunda unidad: Nombre `Almacén Central`, Código `ALM-01`, Tipo `Almacén`, Unidad padre `Dirección de Tecnología`. Si más adelante la organización decide que el almacén dependa directamente de una nueva "Gerencia de Operaciones", basta con usar "Mover a" sobre la fila de "Almacén Central" y seleccionar la nueva unidad padre.

---

## Categorías de activos y campos técnicos personalizados

**Objetivo.** Categorías de activos (`/asset-categories`) es el catálogo que define los **tipos de activo** que la organización administra (laptops, celulares, monitores, servidores, etc.) y, para cada tipo, los **campos técnicos personalizados** que se le piden a cualquier activo de esa categoría (por ejemplo, "RAM (GB)" para laptops o "Número de puertos" para switches).

![Listado de categorías de activos](screenshots/014_categorias_lista.png)

El listado muestra, por categoría: Nombre (con enlace al detalle), Código, Tecnología de identificación por defecto, cantidad de Campos personalizados y Estado. Si falta el permiso correspondiente, el mensaje es: "No tienes permiso para consultar categorías (Catalogs.Read)." **Nota importante:** aunque la entidad se llama "categorías de activos", el permiso que la controla no es uno dedicado, sino el permiso general de catálogos (`Catalogs.Read` / `Catalogs.Create` / `Catalogs.Update`) — ver la tabla completa en §8.

**Configuración — alta de categoría.** El botón "Nueva categoría" abre este formulario:

| Campo | Descripción | Obligatorio | Validaciones |
|---|---|---|---|
| Nombre | Nombre de la categoría (p. ej. "Laptops"). | Sí | Máximo 100 caracteres. |
| Código | Código corto único (p. ej. "LAPTOP"). | Sí | Máximo 50 caracteres. Debe ser único en todo el sistema — si ya existe, el error es: "Ya existe una categoría con el código '{código}'." |
| Tecnología de identificación por defecto | Tecnología de trazabilidad física que se sugiere por defecto para los activos de esta categoría. | Sí (tiene valor por defecto) | Lista desplegable: Código QR / Código de barras / QR y código de barras / NFC / RFID. Valor por defecto: Código QR. |

Si Nombre o Código quedan vacíos, el sistema muestra: "Nombre y código son obligatorios." (En el dominio: "El nombre de la categoría es obligatorio." / "El código de la categoría es obligatorio.")

![Formulario de alta de categoría, vacío](screenshots/016_categorias_nuevo.png)

![Formulario de alta de categoría, completo antes de enviar](screenshots/017_categorias_nuevo_lleno.png)

**Configuración — campos técnicos personalizados.** Una vez creada la categoría, se entra a su detalle (`/asset-categories/[id]`) para definir sus campos técnicos. *[Captura pendiente: pantalla de detalle de categoría de activos]* Esa pantalla muestra un encabezado con el botón Activar/Desactivar de la categoría, una tabla con los campos técnicos ya definidos (Nombre, Código, Tipo, Obligatorio, Opciones) y el formulario "Agregar campo técnico":

| Campo | Descripción | Obligatorio | Validaciones |
|---|---|---|---|
| Nombre | Nombre del campo técnico (p. ej. "RAM (GB)"). | Sí | Máximo 100 caracteres. |
| Código | Código corto del campo, único dentro de la categoría. | Sí | Máximo 50 caracteres. Si se repite dentro de la misma categoría: "Ya existe un campo personalizado con el código '{código}' en esta categoría." |
| Tipo de dato | Naturaleza del valor que se va a capturar. | Sí | Texto / Número / Fecha / Sí-No / Selección. |
| Obligatorio | Si el campo debe completarse siempre al registrar un activo de esta categoría. | No (casilla) | — |
| Opciones | Lista de valores posibles, solo aplica al tipo "Selección". | Sí, únicamente si Tipo de dato = Selección | Si se elige "Selección" sin cargar ninguna opción: "Un campo de selección necesita al menos una opción." |

Si Nombre o Código del campo quedan vacíos: "Nombre y código son obligatorios."

**Valores permitidos.**
- Tecnología de identificación: Código QR, Código de barras, QR y código de barras, NFC, RFID.
- Tipo de dato del campo técnico: Texto, Número, Fecha, Sí/No, Selección (esta última exige al menos una opción).

**Impacto en el sistema.** Los campos técnicos definidos aquí son los que después aparecen en el alta y el detalle de cada activo perteneciente a esa categoría, con la obligatoriedad configurada. Desactivar una categoría no borra los activos ya registrados en ella, pero impide su uso para altas nuevas.

**Limitación conocida:** no existe edición ni borrado de un campo técnico una vez creado. Si se necesita corregir el nombre, el tipo de dato o las opciones de un campo, no hay una vía soportada desde la interfaz para modificarlo — revise cuidadosamente cada campo antes de guardarlo.

**Nota sobre mensajes de error:** un código de categoría duplicado se rechaza con un error de "conflicto" (409), mientras que un código de campo técnico duplicado dentro de la misma categoría se rechaza con un error de "regla de negocio" (422). Ambos casos representan el mismo tipo de problema (una duplicidad), pero el sistema los reporta con clasificaciones distintas; en la práctica, el mensaje en pantalla es igualmente claro en ambos casos, aunque un usuario que integre el sistema con herramientas externas debe tenerlo en cuenta.

**Ejemplo de uso.** Se necesita registrar el modelo, la memoria RAM y si el equipo tiene garantía extendida en cada laptop. Se crea la categoría Nombre `Laptops`, Código `LAPTOP`, Tecnología `Código QR`. Luego, dentro del detalle de esa categoría, se agregan tres campos técnicos: `Modelo` (Texto, obligatorio), `RAM (GB)` (Número, obligatorio) y `Garantía extendida` (Sí/No, no obligatorio). A partir de ese momento, cualquier laptop que se dé de alta en el módulo de Activos bajo la categoría "Laptops" solicitará esos tres datos.

---
