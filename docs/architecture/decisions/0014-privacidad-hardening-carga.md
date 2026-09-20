# ADR 0014 — F12: privacidad/retención, hardening y carga

## Estado
Aceptado (F12, 2026-09-18).

## Contexto
A diferencia de F1-F11 (cada una un módulo funcional para un usuario de negocio), F12 cierra tres
compromisos puramente transversales que el análisis inicial dejó explícitamente para esta fase exacta:

1. `docs/privacy-retention.md` está referenciado desde F0 (§18, punto 7) como "documentado como
   supuesto... cuando se construya F12" — no existía todavía.
2. Revisando `Program.cs`: CORS y el manejo de errores ya estaban endurecidos desde F0, pero no había
   *rate limiting* ni encabezados de seguridad en ningún lado — ni API ni frontend.
3. `docs/testing.md` (escrito en F0) decía literalmente "k6 (o equivalente)... ejecutado en fase F12
   **contra ambiente `test`**" — ese ambiente de Azure (F13) no existe todavía.

## Decisiones

### 1. Privacidad/retención
- **`User.Anonymize(nowUtc)`** (dominio) es un método nuevo, deliberadamente distinto de `Deactivate()`
  (ya existente desde F1): `Deactivate` es reversible y solo revoca acceso; `Anonymize` es irreversible y
  borra PII de verdad (`DisplayName`/`Email` reemplazados por valores sintéticos), gated por
  `Users.Update` ya sembrado — sin permiso nuevo. Requirió una columna nueva, `AnonymizedAtUtc`
  (`DateTimeOffset?`), la única migración de esta fase.
- **No reescribe el pasado**: `AuditEntry.UserDisplayName` (denormalizado a propósito desde F8, ADR 0010,
  específicamente para no perder el nombre en el rastro de auditoría histórico) y cualquier
  `Movement`/`SignatureRecord` que referencie al usuario por `Guid` conservan lo ya escrito. Anonimizar
  detiene la exposición *futura*, no altera registros ya persistidos — mismo principio de inmutabilidad
  que ya rige `AuditEntry`/`Movement` desde sus propias fases.
- **`NotificationRetentionBackgroundService`** (Infraestructura) es el único borrado automático real de
  V1 — mismo patrón de *hosting* que `ImportBatchBackgroundService` (F9: un singleton que resuelve un
  scope por ejecución vía `IServiceScopeFactory`), pero con un `PeriodicTimer` en vez de una cola.
  `Notification` ya se había documentado en F8 como "efímera por diseño" — el candidato seguro para
  purgar de verdad, a diferencia de `Movement`/`AuditEntry` (el registro legal de custodia/acciones, que
  V1 conserva indefinidamente — ver `docs/privacy-retention.md` para el razonamiento completo).

### 2. Hardening
- **Rate limiting**: `Microsoft.AspNetCore.RateLimiting` (ya en el framework desde .NET 7, sin paquete
  nuevo), una política global de ventana fija particionada por `oid` de Entra ID (o IP si no hay sesión).
  El límite (300 solicitudes/10s por defecto, `RateLimiting:PermitLimit`/`WindowSeconds` configurables)
  es deliberadamente generoso: el objetivo es frenar un cliente roto o abusivo, no acotar el uso normal
  ni la propia suite de pruebas (que ya emite ráfagas de cientos de solicitudes por corrida —
  `ApiWebApplicationFactory` sube el límite a un millón para esa suite específicamente). `OnRejected`
  agrega `Retry-After` explícitamente — el limitador de ventana fija no lo hace por sí solo.
- **Encabezados de seguridad**: mismo conjunto base (`X-Content-Type-Options`, `X-Frame-Options`,
  `Referrer-Policy`) en un middleware nuevo de la API (`SecurityHeadersMiddleware`) y en
  `next.config.ts` (`headers()`), más `app.UseHsts()` fuera de `Development` en la API y una
  `Content-Security-Policy` pragmática solo en el frontend (la API es JSON puro, no renderiza HTML — no
  necesita CSP propia). Ver `docs/security/hardening.md` para el detalle completo, incluyendo por qué
  `next/font/google` no requiere allowlist de fuentes externas (se autohospeda en build) y por qué
  `'unsafe-inline'` sigue siendo necesario en V1 (bootstrap de hidratación de Next.js).
- **Fuera de alcance V1**: WAF de borde, protección DDoS administrada, CSP basada en *nonces* — todos
  requieren infraestructura Azure real (F13) o una reestructuración mayor del *rendering* que no se
  justifica para esta fase.

### 3. Carga
- **`LoadTests.cs`** vive dentro del proyecto de integración ya existente, reutilizando
  `ApiWebApplicationFactory` (SQL Server real vía Testcontainers) — sin herramienta ni proyecto nuevo. Se
  eligió deliberadamente sobre instalar k6 (requeriría Docker adicional y, más importante, un mecanismo
  de autenticación de prueba contra el contenedor real de la API que `TestAuthHandler` no expone fuera
  del proceso de pruebas — modificar `Program.cs` para aceptar un esquema de autenticación de prueba
  fuera del entorno de pruebas habría sido un riesgo de seguridad real, no una conveniencia menor) y
  sobre NBomber (licencia condicionada a ingresos, el mismo tipo de riesgo que ya se evitó con PdfSharp
  sobre QuestPDF en F9). `docs/testing.md` ya autorizaba explícitamente un "equivalente" a k6.
- Siembra 2,000 activos reales, ejecuta 5 escenarios (listar, detalle, búsqueda global F11, resumen de
  inventario F10, alta concurrente) con 50 "usuarios" concurrentes reales (`Task.WhenAll` sobre
  `HttpClient`), afirma latencia p95 y cero errores — es a la vez prueba de carga y prueba de regresión
  de rendimiento en CI. La escritura concurrente además verifica que los folios generados sean únicos:
  primer estrés real sobre `EfFolioGenerator` bajo contención genuina en todo el proyecto.
- **Limitación explícita, no una carencia**: esto valida comportamiento bajo concurrencia moderada en un
  proceso local — no la escala de referencia completa del pedido (250k activos, 2M movimientos, 500
  usuarios concurrentes), que requiere infraestructura Azure real y queda para F13. Ver
  `docs/performance.md` para los números reales obtenidos y el razonamiento completo.

## Consecuencias
- Ninguna tabla nueva salvo la columna `AnonymizedAtUtc` en `Users`.
- `docs/privacy-retention.md`, `docs/security/hardening.md` y `docs/performance.md` quedan como las
  referencias vivas para F13 (que deberá revisar rate limiting/headers contra un WAF real, y ejecutar
  `LoadTests.cs`-equivalente contra el ambiente `test` real que F13 construye).
- Verificado: 284 pruebas en verde (230 unitarias + 54 de integración, incluyendo el límite de solicitudes
  real disparando un 429 con `Retry-After`, la anonimización de punta a punta, la purga de notificaciones
  por antigüedad, y la prueba de carga con cero errores).
