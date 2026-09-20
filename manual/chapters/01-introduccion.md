# Manual de Usuario

## Control de versiones

### Versión
1.0 (generada a partir del análisis exhaustivo del código fuente vigente al momento de la redacción)

### Fecha
2026-09-20

### Autor
Documentación generada mediante análisis automatizado del código fuente y verificación funcional directa sobre un entorno real del sistema, con revisión editorial.

### Alcance
Este manual cubre exclusivamente la **Versión 1 (V1)** del sistema, tal como existe hoy en el código: gestión operativa de activos de TI e infraestructura, multiempresa, sin depreciación ni contabilidad de activos (ver "Beneficios" y notas de alcance funcional más abajo). No documenta funcionalidad planeada a futuro que no esté implementada — cuando una pantalla o botón existe pero no está completamente conectado de extremo a extremo, este manual lo señala explícitamente como tal en vez de presentarlo como una función terminada.

Este documento fue construido leyendo el código fuente real de la aplicación (backend en ASP.NET Core 9 y frontend en Next.js 15) como fuente de verdad, contrastándolo contra la documentación técnica existente del proyecto. Donde el comportamiento observado en el código difiere de lo que otra documentación afirma, este manual describe el comportamiento **real** y lo señala como una nota aclaratoria, para que quien lo use no se sorprenda al usar el sistema.

---

# 1. Introducción

## Objetivo del sistema

El sistema es una aplicación empresarial para la **gestión operativa del inventario de activos de TI e infraestructura** de un grupo de empresas: equipos de cómputo, periféricos, equipo de red, y demás activos tecnológicos. Permite dar de alta cada activo con una etiqueta física (código QR, código de barras u otras tecnologías de identificación), llevar su historial completo de movimientos, asignarlo a personas, prestarlo, transferirlo entre empresas del mismo grupo, enviarlo a mantenimiento, controlar sus garantías, y finalmente darlo de baja y disponer de él (venta, donación o destrucción) de forma auditada.

## Alcance funcional

En esta versión, el sistema cubre:

- **Activos**: alta, edición, reubicación, baja y disposición, con etiquetado automático.
- **Inventario y movimientos**: asignaciones a personas, préstamos de corto plazo, transferencias entre empresas del grupo, y una bitácora consolidada de todo movimiento.
- **Mantenimiento**: checklists reutilizables y órdenes de mantenimiento sobre activos concretos.
- **Consumibles, refacciones y garantías**: control de existencias de insumos, ciclo de vida de piezas serializadas, y coberturas de garantía por activo.
- **Solicitudes y aprobaciones**: un mecanismo de autoservicio para que cualquier persona solicite una asignación, un préstamo o un mantenimiento, sujeto a flujos de aprobación configurables por la empresa.
- **Organización e identidad**: administración de empresas, estructura organizacional interna, roles y permisos, y usuarios (que se crean automáticamente al iniciar sesión por primera vez).
- **Datos**: importación masiva de activos desde archivo CSV, exportación a Excel/PDF, reportes operativos, auditoría de acciones sensibles, plantillas de texto, notificaciones internas y búsqueda global.

**Explícitamente fuera de alcance en V1** (por decisión del cliente, documentada en las reglas del proyecto): no hay depreciación contable de activos, no hay gestión financiera ni de costos más allá de campos informativos de captura libre, y no hay cuentas ni contraseñas locales — el acceso es exclusivamente a través de una cuenta de Microsoft Entra ID de la organización.

## Beneficios

- Un registro único y auditable del inventario de TI de todas las empresas del grupo, sin depender de hojas de cálculo.
- Trazabilidad completa: cada movimiento de un activo (asignación, préstamo, transferencia, reubicación, mantenimiento) queda registrado de forma permanente y no se puede editar ni borrar — solo corregir con un nuevo movimiento.
- Control de acceso centralizado a través de la misma cuenta corporativa que ya usan los empleados para correo y demás sistemas de Microsoft 365, sin contraseñas adicionales que administrar.
- Flujos de aprobación configurables por la propia organización, sin depender de que el proveedor del sistema los programe a la medida.

## Perfil de usuarios

El sistema está pensado para tres perfiles de uso, que en la práctica se definen por los permisos que un administrador les asigne (no son roles fijos de fábrica — ver el capítulo "Gestión de usuarios y permisos"):

- **Personal operativo de TI / Activos**: da de alta activos, los asigna, los envía a mantenimiento, gestiona transferencias.
- **Empleados en general (autoservicio)**: consultan los activos que tienen asignados, confirman su recepción, solicitan un préstamo o reportan un equipo a mantenimiento, y participan en aprobaciones si su rol de negocio se lo permite.
- **Administradores**: configuran empresas, estructura organizacional, roles y permisos, dan de alta a otros usuarios, y configuran los flujos de aprobación de su organización.

---

# 2. Requisitos de acceso

## Navegadores compatibles

El sistema es una aplicación web construida con Next.js 15 (React) y no impone restricciones de navegador específicas en el código; funciona en cualquier navegador moderno con soporte de JavaScript actualizado (Chrome, Edge, Firefox, Safari en sus versiones recientes). No se encontró en el código ninguna advertencia de compatibilidad ni bloqueo por user-agent.

## Dispositivos soportados

La interfaz es responsiva: se adapta a partir de un único punto de quiebre (1024 píxeles de ancho). En pantallas menores a ese ancho (celulares y tabletas en posición vertical) el menú de navegación se colapsa en un botón de menú que abre un panel lateral; en pantallas mayores, el menú permanece fijo a la izquierda. Existe además un componente de aplicación web progresiva (service worker) que cachea la estructura visual básica de la aplicación para una carga más rápida en visitas repetidas, aunque no ofrece funcionalidad completa sin conexión a internet — los datos de negocio siempre requieren conexión al servidor.

## Accesos requeridos

Para usar el sistema se necesita:

1. **Una cuenta de Microsoft Entra ID** válida dentro del tenant (organización) configurado para esta aplicación. No existen cuentas locales ni contraseñas propias del sistema — el inicio de sesión es exclusivamente a través del botón "Iniciar sesión con Microsoft".
2. **Que un administrador te haya dado de alta**, o que tu cuenta sea la primera en usar el sistema (ver el procedimiento de arranque inicial en el capítulo "Configuración inicial"). El solo hecho de iniciar sesión crea tu perfil local, pero **no** te da acceso a ninguna empresa ni permiso por sí mismo — eso lo debe conceder un administrador desde la pantalla de usuarios.
3. **Acceso a al menos una empresa** del sistema para poder usar los módulos operativos (activos, movimientos, mantenimiento, etc.). Sin esto, la mayoría de pantallas mostrarán el mensaje: "Todavía no tienes acceso a ninguna empresa. Pide a un administrador que te agregue desde el módulo de usuarios."

## Roles de usuario

El sistema no trae roles predefinidos de fábrica: los roles se configuran libremente desde la pantalla de administración de Roles, asignándoles los permisos que correspondan de un catálogo fijo. La única excepción es la primera cuenta que inicia sesión en la vida del sistema, que recibe automáticamente un rol de superadministrador con todos los permisos existentes, precisamente para poder configurar todo lo demás sin depender de acceso directo a la base de datos. Ver el capítulo "Gestión de usuarios y permisos" para el detalle completo del catálogo de permisos y ejemplos de combinaciones de rol razonables.

## Consideraciones móviles

- La navegación en pantallas pequeñas usa un menú de tipo panel deslizante (drawer) en vez del menú lateral fijo de escritorio.
- Las tablas de los listados ocultan columnas secundarias en pantallas angostas (por ejemplo, categoría o número de serie en el listado de activos) para priorizar la información esencial.
- No se identificó ninguna función exclusiva de escritorio que esté bloqueada en móvil — todas las pantallas son accesibles, aunque su diseño está optimizado primero para escritorio.
