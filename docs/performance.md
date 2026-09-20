# Pruebas de carga (F12)

`docs/testing.md` ya anticipaba esto desde F0: "Carga | k6 (o equivalente) | Escenarios de la sección 31
del pedido, ejecutado en fase F12 **contra ambiente `test`**". Ese ambiente de Azure (F13) no existe
todavía — así que esta fase entrega lo que sí es alcanzable ahora, con honestidad sobre lo que no.

## Alcance real vs. escala de referencia del pedido

El pedido pide validar a **250,000 activos, 2,000,000 movimientos, 500 usuarios concurrentes**. Esa
validación **no se hizo aquí** y no podía hacerse de forma significativa: requiere infraestructura Azure
real (escalado horizontal, *connection pooling* afinado, agentes de carga dedicados), no un contenedor de
SQL Server vía Testcontainers en una laptop. Queda **explícitamente para F13**, contra infraestructura
real.

Lo que sí se entrega: una prueba de carga real, repetible y ejecutable en CI —
`apps/api/tests/AssetManagement.IntegrationTests/LoadTests.cs`, reutilizando el mismo
`ApiWebApplicationFactory` (SQL Server real vía Testcontainers) que el resto de la suite de integración —
sin herramienta ni proyecto nuevo, "equivalente" a k6 tal como `docs/testing.md` ya lo permitía. Valida
comportamiento bajo concurrencia moderada en un solo proceso local, sirve como prueba de regresión de
rendimiento (falla el build si la latencia se degrada gravemente), y **no más que eso**.

## Metodología

1. Siembra directa por `DbContext` (sin pasar por HTTP, para que la siembra en sí no cuente como carga):
   **2,000 activos** reales en una empresa de prueba.
2. Cinco escenarios, cada uno con **50 "usuarios" concurrentes** (`Task.WhenAll` sobre `HttpClient` real,
   atravesando el pipeline completo — autenticación, autorización, EF Core, SQL Server real) x 5
   iteraciones cada uno (salvo el de escritura, una sola iteración con 20 usuarios concurrentes):
   - Listar activos (paginado).
   - Detalle de un activo (aleatorio entre los 2,000 sembrados).
   - Búsqueda global (F11).
   - Resumen de inventario (F10).
   - **Alta de activos concurrente** (20 solicitudes `POST /api/v1/assets` simultáneas) — el primer
     estrés real sobre `EfFolioGenerator` (`MERGE` atómico con reintento, pedido C8) bajo escritura
     concurrente genuina en todo el proyecto.
3. Cada escenario se afirma contra cero errores inesperados y un umbral de p95 generoso (3s para lectura,
   5s para escritura) — pensado para no fallar por ruido del entorno, sí para atrapar una regresión real.
4. Verificación adicional específica de la escritura concurrente: los 20 folios generados deben ser
   **todos únicos** — una colisión indicaría que la lógica de reintento de `EfFolioGenerator` se rompió.

## Resultados de referencia (este entorno: Docker Desktop + Testcontainers, una sola instancia)

| Escenario | Solicitudes | p50 | p95 | p99 | Errores |
|---|---|---|---|---|---|
| Listar activos (paginado) | 250 | 380 ms | 1032 ms | 1280 ms | 0 |
| Detalle de activo | 250 | 363 ms | 413 ms | 429 ms | 0 |
| Búsqueda global | 250 | 588 ms | 754 ms | 851 ms | 0 |
| Resumen de inventario | 250 | 568 ms | 649 ms | 660 ms | 0 |
| Alta de activos concurrente | 20 | 343 ms | 392 ms | 400 ms | 0 |

Sin errores en ningún escenario; los 20 folios de la prueba de escritura concurrente fueron todos únicos.
Estos números son de **este entorno local** (una laptop corriendo Docker Desktop junto con el resto de la
suite) — no representan el rendimiento de un despliegue real; se documentan como línea base de
regresión, no como SLA.

## Ejecutar

```bash
dotnet test apps/api/tests/AssetManagement.IntegrationTests --filter "FullyQualifiedName~LoadTests"
```

## Pendiente (no bloqueante, explícitamente fuera de alcance de V1/F12)

- Validación a la escala de referencia completa del pedido (250k/2M/500 concurrentes) — F13, contra
  infraestructura Azure real.
- Pruebas de carga distribuidas multi-agente (k6 real con múltiples *workers*) — solo tiene sentido una
  vez que exista un ambiente `test` real contra el cual apuntar.
