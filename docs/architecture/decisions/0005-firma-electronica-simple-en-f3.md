# ADR 0005 — F3 construye una firma electrónica simple propia; el motor configurable queda para F4

## Estado
Aceptado (F3, 2026-09-16).

## Contexto
`docs/roadmap.md` listaba la dependencia de F3 ("Inventory Operations") como "F2, motor de firma", pero el
motor de firma electrónica **configurable y multi-mecanismo** es explícitamente un entregable de F4
("Approvals + E-Signature — núcleo"), que además construye el motor de aprobaciones genérico. F3 se
ejecuta antes que F4 en el backlog, así que tomarlo literalmente crearía una dependencia circular.

Sin embargo, `docs/architecture/domain-model.md` ya fijaba, desde el análisis inicial (antes de empezar F3),
un invariante concreto y no negociable: *"`Assignment`: no queda `Assigned` en firme sin `SignatureRecord`
de recepción aceptado"*, y también exige firma para la devolución. Ese invariante tenía que cumplirse en F3
mismo — no podía diferirse a F4 sin dejar una asignación "aceptada" sin ninguna prueba de que el
destinatario la confirmó.

## Decisión
F3 construye una firma electrónica **simple, de un solo mecanismo** (`SignatureRecord.Mechanism` fijo en
`"TypedConfirmation"`): la persona escribe su nombre completo como acto deliberado de confirmación, y el
sistema captura junto con eso el `SignerUserId` (identidad autenticada real, no autodeclarada), fecha, IP,
user-agent y un hash SHA-256 del contenido firmado — exactamente lo que describe la sección C6/§18 del
pedido original ("firma electrónica simple registra IP/dispositivo/hash").

Esto se usa en dos puntos de F3: `SignAssignmentCommand` (recepción, autoservicio — firma el propio
destinatario) y `ReturnAssignmentCommand` (devolución — firma quien ejecuta la devolución, típicamente
TI/almacén). `Loan` deliberadamente **no** requiere firma (ver decisión de alcance en el plan de F3):
`domain-model.md` solo exige firma para `Assignment`, no para `Loan` — un préstamo es informal y de corto
plazo, una asignación es el resguardo formal de largo plazo.

F4, cuando se construya, **generaliza** este mecanismo (firma dibujada, plantillas versionadas, más
mecanismos) sin romper el esquema — `SignatureRecord.Mechanism` ya es un campo abierto, no un enum cerrado
a un solo valor — y agrega por separado el motor de aprobaciones genérico (`ApprovalFlowDefinition` /
`ApprovalInstance`), que F3 no necesita: nada en el alcance de F3 requiere aprobación multi-nivel, solo la
firma de quien tiene el activo físicamente en sus manos.

## Consecuencias
- F3 no depende de F4 para cumplir sus propios invariantes de dominio ya documentados.
- El esquema de `SignatureRecord` (contexto polimórfico `ContextType`/`ContextId`, igual que
  `ApprovalInstance`) ya está listo para que F4 agregue mecanismos sin migrar datos existentes.
- Simplificación deliberada, no omisión: no hay flujo de autoservicio para la firma de devolución en V1 —
  la persona que ejecuta la devolución firma en un solo paso. Ver `docs/roadmap.md` para el resto de
  pendientes documentados de F3.
