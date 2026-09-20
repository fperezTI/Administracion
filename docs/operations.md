# Operación

## Observabilidad

- **Serilog** estructurado (JSON) para logs técnicos, sin PII (políticas de *destructuring*/scrubbing
  explícitas para campos como email, nombre, IP en el log técnico — esa información vive en `AuditEntry`,
  no en logs).
- **OpenTelemetry** para trazas y métricas, exportadas a **Application Insights** en Azure.
- `CorrelationId` propagado por middleware desde el primer request (header `X-Correlation-Id` o generado) y
  presente tanto en logs técnicos como en `AuditEntry`.

## Manejo de errores

Middleware centralizado que traduce excepciones de dominio/aplicación a `ProblemDetails` (RFC 7807) con
códigos HTTP apropiados (400 validación, 401/403 auth, 404 no encontrado, 409 conflicto de concurrencia, 422
regla de negocio, 500 no controlado — nunca con detalle interno expuesto al cliente en `prod`).

## Salud y diagnóstico

Endpoints `/health/live` y `/health/ready` (chequeo de SQL y Blob Storage) vía `Microsoft.Extensions.
Diagnostics.HealthChecks`, usados por App Service y por el pipeline de despliegue para validar antes de
enrutar tráfico.

## Migraciones en operación

Ver `docs/architecture/00-analysis.md` §16. Nunca `EnsureCreated`/migración automática silenciosa en el
arranque de `prod`. Aplicación de migraciones como paso explícito y auditable del pipeline, con script SQL
revisado previamente.

## Runbooks (se completan a partir de F13 con datos reales de Azure)

Pendiente: procedimiento de rollback de despliegue, procedimiento de rotación de secretos en Key Vault,
procedimiento de purga por política de retención, procedimiento de reactivación excepcional de activo dado
de baja (auditado).
