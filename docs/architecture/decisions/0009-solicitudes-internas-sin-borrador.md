# ADR 0009 — Solicitudes internas: sin fase de borrador, cumplimiento automático, `Requests.Update` sin usar

## Estado
Aceptado (F7, 2026-09-17).

## Contexto
`docs/architecture/domain-model.md` reservó desde el inicio la fila `InternalRequest | Requests | Máquina
de estados propia (borrador→...→cerrada)`. Al construir F7 aparecieron tres ambigüedades reales:

1. La palabra "borrador" sugiere una fase de edición antes de enviar a aprobación — pero ningún otro
   flujo tipo-solicitud ya construido (`Transfer`, `Loan`, `MaintenanceOrder`) tiene una fase así; todos
   van de "crear" directo a "pendiente".
2. El roadmap describe la integración con una frase concreta: *"integración con F4 para generar
   asignaciones/movimientos automáticamente"* — pero no especifica si eso ocurre automáticamente al
   aprobarse o requiere un paso manual posterior de alguien que "cumple" la solicitud.
3. El catálogo de permisos sembró `Requests.Read/Create/Update` desde F1, pero cancelar una solicitud
   pendiente podría ser autoservicio (como `CancelTransferCommand`) o una acción administrativa (como
   `CancelPendingAssignmentCommand`, que sí usa `Assignments.Update`) — ambos patrones ya conviven en el
   código.

## Decisión

- **Sin fase de borrador editable.** `InternalRequest.Create(...)` entra directo en `PendingApproval`,
  mismo criterio de un solo paso que `Transfer`/`Loan`/`MaintenanceOrder`. "Borrador→...→cerrada" se lee
  como la forma general de "empieza en algún lado, termina en un estado cerrado" (`Rejected`, `Cancelled`
  o `Fulfilled` son los tres "cerrada" posibles), no como un mandato de pantalla de edición previa.
  Cancelar una solicitud pendiente (autoservicio) cubre el caso de "me equivoqué, la retiro" sin necesidad
  de una fase de edición separada.
- **Cumplimiento automático**, tal como dice literalmente el roadmap ("automáticamente"): un nuevo
  `InternalRequestApprovalReactionHandler` reacciona a `ApprovalCompleted` (mismo esqueleto que
  `TransferApprovalReactionHandler` de F5) y reconstruye, según `InternalRequest.Type`, la misma secuencia
  de dominio que ya ejecutan `CreateAssignmentCommand`/`CreateLoanCommand`/`OpenMaintenanceOrderCommand` —
  sin volver a invocar esos comandos vía `ISender` (ningún reaction handler de F4/F5 lo hace). El
  solicitante es siempre el beneficiario (`RequestedByUserId`): es autoservicio, "pido que me asignen X",
  no una forma de originar una acción para otra persona.
- **Tres flujos de aprobación, uno por `InternalRequestType`** (`internal-request.asset-assignment`,
  `internal-request.loan`, `internal-request.maintenance`) — un administrador real querría aprobadores
  distintos según el tipo, mismo criterio que separó `asset.decommission` de `asset.disposal` en F4.
- **Cancelar es autoservicio, sin permiso** — mismo patrón que `CancelTransferCommand`/
  `CancelApprovalInstanceCommand` (F4/F5), no el de `CancelPendingAssignmentCommand` (que es una acción
  administrativa distinta, sobre asignaciones ya creadas, no sobre la propia solicitud de alguien). Al
  igual que `CancelTransferCommand`, no cancela también la `ApprovalInstance` asociada — mismo precedente
  ya aceptado, la instancia queda `Pending` sin nada que decidir.
- **`Requests.Update` queda sin usar en V1**, documentado, no omitido: una vez que no hay fase de edición
  de borrador y cancelar es autoservicio sin permiso, no aparece una acción real de "actualizar" que lo
  necesite — análogo a como F6 dejó `UnderWarranty` sin conectar. `Requests.Create` protege el envío
  (`CreateInternalRequestCommand`) y `Requests.Read` protege el listado/detalle administrativo
  (`GetInternalRequestsQuery`/`GetInternalRequestByIdQuery`) — el propio solicitante ve sus solicitudes sin
  ese permiso vía `GetMyInternalRequestsQuery` (autoservicio, mismo patrón que `GetMyAssignmentsQuery`).

## Consecuencias
- Mismo riesgo ya aceptado en F4/F5: si el estado del activo cambió entre el envío y la aprobación (p.
  ej. alguien más lo tomó), el reaction handler lanza `DomainException` y la decisión de aprobación en sí
  ya quedó guardada — comportamiento heredado, no nuevo de esta fase.
- `docs/architecture/domain-model.md` se actualiza para reflejar esta resolución.
- Verificado por integración: los tres tipos de solicitud, aprobados, crean exactamente el
  `Assignment`/`Loan`/`MaintenanceOrder` esperado y mueven el activo al estado correspondiente; el rechazo
  deja el activo sin cambios; la cancelación por el propio solicitante funciona y queda registrada.
