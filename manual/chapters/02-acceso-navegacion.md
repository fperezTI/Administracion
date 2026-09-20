# 3. Acceso al sistema

## Inicio de sesión

### Pantalla

Al abrir el sistema sin haber iniciado sesión, se muestra una página de bienvenida con el título
**"Gestión de Activos de TI e Infraestructura"** y el subtítulo **"Registro, etiquetado y control
de activos de TI e infraestructura en todas tus empresas."**

En esta pantalla (ver captura ![Captura de pantalla](screenshots/001_landing_no_autenticado.png)) se puede observar:

- Un indicador del estado del servicio (un punto de color verde o rojo) acompañado de un texto con
  el formato `{producto} — {entorno} — {hora del servidor en formato es-MX}`. Este indicador
  consulta al API de forma anónima al cargar la página. Si el API no responde, el punto se muestra
  en rojo y aparece el mensaje **"No fue posible contactar al API."**
- Un selector de tema claro/oscuro, visible incluso sin haber iniciado sesión.
- Un botón para iniciar sesión: **"Iniciar sesión con Microsoft"**.

Al presionar el botón de inicio de sesión, el sistema redirige a la pantalla de autenticación de
Microsoft Entra ID (ver captura ![Captura de pantalla](screenshots/002_login_microsoft.png)), donde la persona usuaria
ingresa sus credenciales corporativas. Si el tenant de la organización exige verificación en dos
pasos (MFA), Microsoft la solicitará en este mismo flujo, fuera del sistema.

Si la persona ya tiene una sesión activa y regresa a la página de inicio, en lugar del botón de
inicio de sesión verá un botón **"Ir a mi dashboard"**, que la lleva directamente al panel
principal sin pasar de nuevo por Microsoft.

### Campos

El sistema **no tiene formulario propio de usuario y contraseña**. Toda la autenticación se
delega por completo a Microsoft Entra ID (inicio de sesión federado). Esto significa que:

- No existen campos de "usuario" ni "contraseña" dentro de la aplicación.
- No hay opción de "recuperar contraseña" ni de "crear cuenta" — ambas se gestionan, si aplica,
  desde la organización en Microsoft Entra ID, fuera de este sistema.
- El único control disponible en la pantalla de inicio de sesión del sistema es el botón
  **"Iniciar sesión con Microsoft"**, que dispara el flujo de autenticación federada hacia
  Microsoft (protocolo OAuth con inicio de sesión único).

### Validaciones

El sistema realiza las siguientes verificaciones automáticas, sin intervención de la persona
usuaria:

- Al volver de Microsoft con una sesión válida, el sistema consulta el perfil del usuario en el
  API (`GET /api/v1/me`) para obtener sus permisos y las empresas a las que tiene acceso.
- Si es la primera vez que la persona inicia sesión, el sistema crea automáticamente su perfil
  interno, pero **sin ninguna empresa ni rol asignado todavía** — eso lo debe configurar un
  administrador después.
- Si la persona ya había iniciado sesión antes, el sistema actualiza su nombre, correo y fecha de
  último acceso con la información más reciente de Microsoft.
- Si no hay una sesión válida (o si falló la renovación automática de la sesión) al intentar
  entrar al dashboard, el sistema regresa automáticamente a la página de inicio.

### Errores posibles

- **Cuenta desactivada**: si un administrador desactivó la cuenta de la persona usuaria, el
  sistema corta el acceso con el mensaje **"La cuenta de usuario está desactivada."**
- **Error al renovar la sesión**: si la sesión expiró y no fue posible renovarla automáticamente,
  el sistema regresa a la persona a la página de inicio. En este caso particular **no se muestra
  un mensaje explícito de "tu sesión expiró"** — la pantalla de inicio se ve igual que en una
  visita normal, sin distinguir el motivo del regreso. Es una limitación conocida del sistema: si
  usted es enviado inesperadamente a la pantalla de inicio, intente iniciar sesión de nuevo.
- **Cuenta sin empresas asignadas**: no es un error propiamente, pero si la cuenta es nueva y aún
  no tiene ninguna empresa asignada, el dashboard aparecerá vacío (ver "Casos especiales" en la
  sección de Dashboard).

### Procedimiento paso a paso

1. Abra la dirección del sistema en su navegador. Se mostrará la página de inicio
   (![Captura de pantalla](screenshots/001_landing_no_autenticado.png)).
2. Verifique, si lo desea, el indicador de estado del API en la parte visible de la pantalla.
3. Presione el botón **"Iniciar sesión con Microsoft"**.
4. El navegador lo redirige a la pantalla de autenticación de Microsoft
   (![Captura de pantalla](screenshots/002_login_microsoft.png)). Escriba su correo y contraseña corporativos.
5. Si su organización tiene habilitada la verificación en dos pasos, complétela cuando Microsoft
   se la solicite.
6. Microsoft lo regresa automáticamente al sistema, ya autenticado.
7. El sistema lo redirige a `/dashboard`.
   - **Si es su primer inicio de sesión**: el sistema crea su perfil interno en ese momento, pero
     usted todavía no tiene ninguna empresa ni permiso asignado. Verá el dashboard
     **completamente en blanco** (a propósito) — ver captura ![Captura de pantalla](screenshots/003_dashboard.png), que
     muestra el menú completo pero sin datos ni indicadores. Deberá solicitar a un administrador
     que le asigne una empresa y un rol desde el módulo de usuarios.
   - **Si ya ha iniciado sesión antes y tiene empresas/roles asignados**: el dashboard mostrará los
     indicadores con datos reales, como en la captura ![Captura de pantalla](screenshots/057_dashboard_con_datos.png).
8. En sesiones posteriores, si usted ya tiene una sesión activa y visita la página de inicio,
   bastará con presionar **"Ir a mi dashboard"** para volver a entrar, sin repetir el paso de
   Microsoft.
9. Para salir del sistema, use el botón **"Cerrar sesión"** de la barra superior (ver sección
   "Barra superior" más abajo).

# 4. Navegación general

## Menú principal

El menú principal se organiza en grupos plegables tipo acordeón, donde solo un grupo permanece
abierto a la vez. Además del menú agrupado, existe una sección de autoservicio siempre visible
para cualquier persona que haya iniciado sesión.

> **Nota importante**: el menú **no oculta opciones según los permisos de la persona usuaria** —
> todas las personas ven todas las opciones del menú, sin importar si tienen permiso para usarlas
> o no. El control de acceso real ocurre al entrar a cada página: si no se tiene el permiso
> correspondiente, la página responderá con un error de acceso denegado (403). Es decir, ver una
> opción en el menú no garantiza poder usarla.

| Grupo | Opciones incluidas |
|---|---|
| Activos | Activos, Categorías, Garantías |
| Inventario y movimientos | Asignaciones, Préstamos, Movimientos, Transferencias, Refacciones, Consumibles |
| Mantenimiento | Mantenimiento, Checklists |
| Solicitudes y aprobaciones | Solicitudes, Flujos de aprobación |
| Datos | Importaciones, Reportes, Plantillas |
| Administración | Empresas, Estructura, Roles, Usuarios, Auditoría |

## Menús secundarios / autoservicio

Independientemente del grupo anterior, toda persona autenticada ve siempre, sin filtrar por
permisos, las siguientes opciones de autoservicio, pensadas para que cada quien gestione lo que
le corresponde de forma personal:

- Mis asignaciones
- Mis solicitudes
- Mis aprobaciones
- Mis notificaciones

## Barra superior

La barra superior (visible en todas las pantallas internas del sistema) incluye:

- **Logo "AT"**: aparece únicamente en pantallas angostas (menores al punto de quiebre móvil,
  ver sección de navegación móvil).
- **Título y subtítulo** de la sección actual.
- **Buscador**: permite buscar por término dentro del sistema.
- **Selector de empresa activa** (`CompanySwitcher`): aparece únicamente en las páginas que lo
  incluyen (alrededor de 20 pantallas de listado y alta de registros). No aparece en el
  `/dashboard` (que siempre muestra datos consolidados de todas las empresas, ver sección 5) ni en
  `/audit` (porque la auditoría es un permiso a nivel de todo el sistema, no ligado a la
  membresía en una empresa en particular).

  > **Nota aclaratoria**: aunque cierta documentación técnica interna describe el cambio de
  > empresa activa como algo pendiente o basado en un mecanismo distinto, en la práctica el
  > selector de empresa **sí existe y funciona**, pero lo hace de una manera particular: el
  > cambio de empresa se guarda como parte de la dirección web (`?companyId=` en la URL) y se
  > gestiona enteramente en el navegador de la persona usuaria, no mediante el mecanismo que otra
  > documentación del proyecto podría hacer suponer. En la práctica, para usted como persona
  > usuaria esto no cambia la forma de usarlo: seleccione la empresa en el desplegable y la
  > página recargará su contenido filtrado a esa empresa. Solo tenga en cuenta que si comparte o
  > guarda el enlace de una página con una empresa seleccionada, ese enlace "recuerda" la empresa
  > elegida.

- **Selector de tema** (`ThemeToggle`): alterna entre tema claro y oscuro. Es un interruptor de
  solo dos posiciones (claro/oscuro); no existe todavía, aunque se planeó, una tercera opción para
  "seguir el tema del sistema operativo".
- **Botón "Cerrar sesión"**: al presionarlo, el sistema cierra la sesión local y además redirige a
  Microsoft para cerrar también la sesión en Entra ID (lo que se conoce como cierre de sesión
  federado). Esto significa que cerrar sesión en el sistema también puede cerrar su sesión general
  de Microsoft, dependiendo de la configuración de su organización.

  > **Nota aclaratoria**: cierta documentación de seguridad del proyecto indica que este cierre de
  > sesión federado está pendiente de implementar. En la práctica, la funcionalidad ya está
  > activa: al cerrar sesión, sí se invalida también la sesión en Microsoft Entra ID.

Debajo de la barra superior siempre se muestra la barra/menú de navegación principal descrita
arriba.

## Acciones rápidas

El buscador de la barra superior funciona como acción rápida disponible desde cualquier pantalla
interna, permitiendo localizar información por término de búsqueda sin necesidad de navegar por
los menús. Fuera de esto, el reporte de análisis no documenta botones de "acciones rápidas"
adicionales (por ejemplo, accesos directos para crear un activo desde cualquier pantalla); si su
instalación cuenta con ellos, no están cubiertos por este capítulo.

## Navegación móvil

El sistema tiene un único punto de quiebre (breakpoint) para adaptar la navegación a pantallas
pequeñas, ubicado en 1024 píxeles de ancho:

- **Pantallas menores a 1024px** (celulares y tablets en vertical): el menú lateral permanece
  oculto por defecto. Aparece un botón de tipo "hamburguesa" que, al presionarse, abre un panel
  deslizante (drawer) desde el lado izquierdo de la pantalla con todas las opciones de
  navegación. Este panel se cierra automáticamente en cuanto se selecciona cualquier opción de
  navegación.
- **Pantallas de 1024px en adelante** (escritorio/laptop): el menú lateral se muestra siempre
  fijo, con un ancho de 224 píxeles, sin necesidad de abrirlo manualmente.
- El logotipo "AT" en la barra superior solo se muestra en las pantallas angostas (menores a
  1024px); en pantallas de escritorio no aparece porque el menú lateral ya está visible.

El sistema también cuenta con una capacidad básica de funcionamiento sin conexión limitada a la
apariencia general de la aplicación (no a los datos): guarda en caché los elementos visuales de la
interfaz (como el menú y la estructura de las páginas) para que la aplicación cargue más rápido,
pero **no guarda en caché la información de activos, movimientos ni ningún otro dato de negocio**.
Esto solo aplica al entorno de producción.

## Dashboard

El dashboard (panel principal) es la primera pantalla que se muestra después de iniciar sesión y
requiere sesión activa. Su contenido depende del permiso **"Dashboards.ViewExecutive"**:

- **Si la persona usuaria cuenta con este permiso**, el sistema consulta los indicadores
  ejecutivos y muestra siete tarjetas de indicador (KPI):

  1. Total de activos
  2. Activos asignados
  3. Activos disponibles
  4. Aprobaciones pendientes (se resalta en color de advertencia cuando hay alguna pendiente)
  5. Mantenimientos abiertos
  6. Garantías por vencer (se resalta en color de advertencia cuando hay garantías que vencen
     dentro de los siguientes 30 días)
  7. Existencias bajas (se resalta en color de alerta/crítico cuando hay existencias por debajo
     del mínimo)

- **Si la persona usuaria no cuenta con este permiso**, no se muestran estos indicadores.

Compare la captura ![Captura de pantalla](screenshots/003_dashboard.png) (dashboard inmediatamente después del primer
inicio de sesión, sin ninguna empresa ni dato configurado — se ve el menú completo, pero sin
indicadores) contra la captura ![Captura de pantalla](screenshots/057_dashboard_con_datos.png) (dashboard de una cuenta
con al menos una empresa y datos ya cargados, mostrando las siete tarjetas con valores).

**Punto importante sobre el alcance de los datos**: el dashboard ejecutivo **siempre muestra
información consolidada de todas las empresas a las que la persona usuaria tiene acceso**. A
diferencia de otras pantallas del sistema (listados de activos, asignaciones, etc.), el dashboard
**no ofrece un selector de empresa** y no existe manera, desde la interfaz, de acotar estos
indicadores a una sola empresa. Si usted tiene acceso a varias empresas, los totales que ve en el
dashboard son la suma de todas ellas juntas — no de una empresa individual. Actualmente la
pantalla tampoco incluye ningún aviso visual que le recuerde que se trata de una vista
consolidada, así que tome en cuenta esta particularidad al interpretar los números.

**Casos especiales**: Si su cuenta no tiene ninguna empresa asignada todavía, el dashboard se
mostrará vacío, sin los indicadores (esto ocurre por no contar con el permiso correspondiente,
sin un aviso explícito distinto al de una cuenta común sin permisos). En pantallas de listado de
información (no en el dashboard), el sistema sí muestra explícitamente el mensaje "Todavía no
tienes acceso a ninguna empresa. Pide a un administrador que te agregue desde el módulo de
usuarios." Si su cuenta no cuenta con el permiso "Dashboards.ViewExecutive", simplemente no verá
las tarjetas de indicadores, sin un mensaje de error adicional. Si su cuenta fue desactivada por
un administrador, al intentar consultar el dashboard verá el mensaje "Tu cuenta está desactivada.
Contacta a un administrador." Si ocurre cualquier otro problema al consultar el dashboard, se
mostrará el mensaje "No fue posible consultar el dashboard en el API." Por último, si su sesión
expira mientras usa el sistema, el mecanismo de renovación automática intentará mantenerla activa
de forma transparente; si eso falla, será enviado de regreso a la página de inicio sin un aviso
explícito de que su sesión expiró.
