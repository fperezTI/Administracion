# ADR 0006 — El motor de aprobaciones usa roles dinámicos, no listas fijas de personas

## Estado
Aceptado (F4, 2026-09-16).

## Contexto
`docs/architecture/domain-model.md` bocetó el motor de aprobaciones desde el análisis inicial (antes de
F4) con esta forma:

```
ApprovalFlowDefinition
  ApproverRoles: Role[]
  RequiredApprovals: int
  Mode: Sequential | Parallel
  Threshold: Unanimous | Minimum(n)
```

Al implementar F4 surgieron dos ambigüedades reales que ese boceto no resolvía:

1. **`ApproverRoles` son roles (RBAC), no personas fijas** — igual que el resto del sistema, la
   pertenencia a un rol es dinámica (alguien puede ganar o perder el rol después de creado el flujo). Con
   un pool dinámico, `Threshold: Unanimous` no está bien definido: ¿unánime entre quiénes, si el conjunto
   de aprobadores elegibles puede cambiar en cualquier momento?
2. **¿Qué significa "secuencial" sin una lista fija de aprobadores?** Si los aprobadores son "cualquiera
   con el rol X", el orden no puede ser "la persona A antes que la persona B" — tiene que ser un orden
   entre **roles**.

## Decisión
- Se elimina `Threshold` del diseño. Solo queda **`RequiredApprovals: int`** — el "N" de "N-de-M" que pide
  C4 (`00-analysis.md`) literalmente. En modo `Parallel`, varias personas distintas pueden compartir el
  mismo rol, así que `RequiredApprovals` puede exceder la cantidad de roles listados (p. ej.
  `roles=[Manager]`, `requiredApprovals=2` pide dos gerentes distintos, cada uno con su propia decisión).
- **Secuencial = orden entre roles, no entre personas**: `ApproverRoleIds` es una lista ordenada; el paso
  *i* solo se habilita cuando el paso *i-1* ya tiene una aprobación registrada (de cualquier persona con
  ese rol). En modo `Sequential`, `RequiredApprovals` siempre es igual a la cantidad de roles listados —
  una aprobación por rol, en orden.
- **Un solo rechazo termina la instancia** de inmediato, sin ponderar contra aprobaciones previas — evita
  diseñar conteo de votos negativos, y es el comportamiento esperado en un control de este tipo.
- **Elegibilidad se resuelve en el momento de decidir**, consultando `UserRole` + membresía de empresa —
  igual que cualquier otro chequeo de RBAC en el sistema (nunca se "congela" una lista de aprobadores al
  crear la instancia, solo se congela QUÉ roles cuentan).

## Consecuencias
- `ApprovalInstance` snapshotea `Mode`/`RequiredApprovals`/`ApproverRoleIds` de la definición al crearse,
  así que editar la definición después nunca afecta una instancia en curso (se desactiva y se crea una
  nueva, mismo patrón que otros catálogos de este sistema).
- Sustitución temporal y escalamiento automático (V1.2, ver C4) encajan en este diseño sin cambiar el
  esquema: son formas de ampliar temporalmente quién cuenta como "tiene el rol X", no un cambio a
  `ApprovalFlowDefinition`/`ApprovalInstance`.
- `GetMyPendingApprovalsQuery` no puede expresar "de quién es el turno" en una sola consulta SQL sobre una
  colección de roles ordenada — se resuelve materializando el conjunto (acotado, por empresa) de
  instancias pendientes y evaluando el turno en memoria. Aceptable para V1; se revisará si el volumen real
  lo justifica.
