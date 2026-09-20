# 9. Flujo operativo completo

Los diagramas de esta sección integran, en una sola vista, los procesos que los capítulos
anteriores documentaron por separado. Sirven como mapa de referencia rápida: no repiten el detalle
de campos, validaciones ni mensajes (ya cubiertos en cada capítulo), solo muestran cómo se
encadenan las pantallas y las decisiones.

## Del primer acceso a la operación diaria

```mermaid
flowchart TD
    A[Inicio: abrir la aplicación] --> B[Iniciar sesión con Microsoft]
    B --> C{¿Es el primer login\nen la vida del sistema?}
    C -->|Sí| D[Se crea automáticamente\nun superadministrador\ncon todos los permisos]
    D --> E[Crear la primera empresa\nen Empresas]
    E --> F[Otorgarse acceso a esa empresa\ndesde Usuarios]
    F --> G[Configurar estructura organizacional,\nroles y categorías de activos]
    C -->|No| H{¿Tiene empresa\ny permisos asignados?}
    H -->|No| I[Pantalla muestra: 'Todavía no tienes acceso a ninguna empresa']
    I --> J[Pedir a un administrador\nque otorgue acceso]
    H -->|Sí| K[Dashboard con datos\nde las empresas accesibles]
    G --> K
    J --> H
    K --> L[Operar los módulos:\nActivos, Movimientos,\nMantenimiento, Solicitudes, Datos]
```

## Ciclo de vida completo de un activo

```mermaid
flowchart TD
    A[Alta del activo\no importación masiva] --> B[Activo en almacén]
    B --> C{¿Qué se necesita hacer?}
    C -->|Asignar a una persona| D[Crear asignación]
    D --> E[Persona firma\nrecepción]
    E --> F[Activo asignado]
    F --> G[Devolución]
    G --> B
    C -->|Prestar brevemente| H[Crear préstamo]
    H --> I[Activo prestado]
    I --> J[Devolución del préstamo]
    J --> B
    C -->|Enviar a mantenimiento| K[Abrir orden de mantenimiento]
    K --> L[Activo en mantenimiento]
    L --> M{Resultado del cierre}
    M -->|Reparado| B
    M -->|No se pudo reparar| N[Activo dañado]
    N --> O[Solicitar baja]
    C -->|Transferir a otra empresa| P[Solicitar transferencia]
    P --> Q[Aprobación del flujo\nconfigurado]
    Q -->|Aprobada| R[Activo en tránsito]
    R --> S[Empresa destino confirma\nrecepción con firma]
    S --> T[Activo en almacén\nde la nueva empresa]
    Q -->|Rechazada| B
    C -->|Ya no se usará más| O
    O --> U{Aprobación de baja}
    U -->|Rechazada| B
    U -->|Aprobada| V[Activo dado de baja]
    V --> W[Solicitar disposición:\nventa, donación o destrucción]
    W --> X{Aprobación de disposición}
    X -->|Rechazada| V
    X -->|Aprobada| Y[Activo vendido / donado / destruido]
```

## Solicitud de autoservicio → aprobación → cumplimiento

```mermaid
flowchart TD
    A[Empleado abre 'Nueva solicitud'] --> B{Tipo de solicitud}
    B -->|Asignación de activo| C[Elige un activo\nen almacén]
    B -->|Préstamo| D[Elige un activo\nen almacén + fecha\nde devolución]
    B -->|Mantenimiento| E[Elige un activo\nen almacén o asignado]
    C --> F[Se crea la solicitud\ny automáticamente\nuna aprobación pendiente]
    D --> F
    E --> F
    F --> G[La aprobación aparece\nen 'Mis aprobaciones'\nde cada rol elegible]
    G --> H{Decisión}
    H -->|Rechaza| I[Solicitud rechazada.\nEl activo no cambia de estado]
    H -->|Aprueba\n- puede requerir varias\naprobaciones según el flujo| J{¿Se completaron\ntodas las aprobaciones\nrequeridas?}
    J -->|No todavía| G
    J -->|Sí| K{Tipo de solicitud}
    K -->|Asignación| L[Se crea la asignación\ny el movimiento asociado.\nEl activo queda reservado\nhasta que el destinatario firme]
    K -->|Préstamo| M[Se crea el préstamo\ny el activo queda prestado]
    K -->|Mantenimiento| N[Se abre la orden\nde mantenimiento correctivo]
    L --> O[Solicitud cumplida]
    M --> O
    N --> O
```

## Notas de lectura de estos diagramas

- Las decisiones marcadas como "Aprobación" dependen de que la empresa tenga configurado un flujo
  de aprobación para ese tipo de operación (ver capítulo "Solicitudes y aprobaciones"); sin un flujo
  configurado, el sistema no permite crear la solicitud o la operación correspondiente.
- Cancelar una solicitud propia **no** cierra automáticamente la aprobación pendiente asociada —
  ver la nota correspondiente en el capítulo de Solicitudes y aprobaciones.
- El diagrama de ciclo de vida del activo es una simplificación de la máquina de estados completa
  documentada en el capítulo "Activos" (que incluye además los estados Reservado, Extraviado,
  Robado y En garantía) — consulta ese capítulo para el detalle exhaustivo de las 15 situaciones
  posibles y sus transiciones exactas.
